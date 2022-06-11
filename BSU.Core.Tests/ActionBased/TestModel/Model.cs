using System;
using System.Collections.Generic;
using System.Threading;
using BSU.Core.Concurrency;
using BSU.Core.Events;
using BSU.Core.Ioc;
using BSU.Core.Model;
using BSU.Core.Persistence;
using BSU.Core.Services;
using BSU.Core.Tests.Mocks;
using BSU.Core.ViewModel;
using BSU.CoreCommon;
using NLog;
using Xunit;

namespace BSU.Core.Tests.ActionBased.TestModel;

internal class Model : IDisposable
{
    // idea: have a user thread and a model thread.
    // That way the test can be written linearly and suspend the model instead of having to deal with callbacks en masse

    private readonly Thread _modelThread;

    private bool _shutDown;

    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    private readonly AutoResetEvent _modelThreadSuspended = new(false);
    private readonly AutoResetEvent _modelThreadContinue = new(false);
    private Action? _modelThreadAction;
    private Exception? _modelThreadException;
    private readonly TestDispatcher _dispatcher = new();

    private readonly Dictionary<string, TestRepository> _repositories = new();
    private readonly Dictionary<string, TestStorage> _storages = new();

    public readonly List<ErrorEvent> ErrorEvents = new();
    private readonly TestModelInterface _testModelInterface;

    private ModelActionContext _currentContext = null!;

    public readonly ServiceProvider Services = new();

    private record ModelBuildParams(IEnumerable<string> Repositories, IEnumerable<string> Storages, bool SteamLoaded);

    public Model(IEnumerable<string>? repositories = null, IEnumerable<string>? storages = null, bool steamLoaded = true)
    {
        var buildParams = new ModelBuildParams(repositories ?? Array.Empty<string>(), storages ?? Array.Empty<string>(), steamLoaded);

        _testModelInterface = new TestModelInterface(DoInModelThreadWithWait);
        _modelThread = new Thread(ModelThread);
        _modelThread.Start(buildParams);
        _modelThreadSuspended.WaitOne();
        if (_modelThreadException != null)
            throw new ModelFailedException(_modelThreadException);
    }

    private void DoInModelThreadWithWait(Action action, bool wait)
    {
        Do(() =>
        {
            action();
            if (wait) _dispatcher.WaitForWork();
        });
    }

    private void CheckException()
    {
        if (_modelThreadException != null)
            throw new ModelFailedException(_modelThreadException);
    }

    private object BuildModel(ModelBuildParams modelBuildParams)
    {
        var eventManager = new EventManager();

        var asyncVoidExecutor = new TestAsyncVoidExecutor(eventManager);

        Services.Add<IAsyncVoidExecutor>(asyncVoidExecutor);
        eventManager.Subscribe<ErrorEvent>(e => ErrorEvents.Add(e));

        var interactionService = new TestInteractionService(HandleInteraction);
        Services.Add<IInteractionService>(interactionService);
        Services.Add<IDispatcher>(_dispatcher);
        Services.Add<IEventManager>(eventManager);
        Services.Add<IModActionService>(new ModActionService());
        Services.Add<IStorageService>(new StorageService());
        Services.Add<IConflictService>(new ConflictService(Services));
        Services.Add<IAutoSelectionService>(new AutoSelectionService(Services));
        Services.Add<IErrorService>(new ErrorService(Services));
        Services.Add<IRepositoryStateService>(new RepositoryStateService(Services));
        Services.Add<IDialogService>(new DialogService(Services));
        var types = new Types();

        types.AddRepoType("BSO", CreateRepository);
        types.AddStorageType("DIRECTORY", p => CreateStorage(p, false));
        types.AddStorageType("STEAM", p => CreateStorage(p, true));

        Services.Add(types);

        var settings = new MockSettings();
        foreach (var repository in modelBuildParams.Repositories)
        {
            settings.Repositories.Add(new RepositoryEntry(repository, "BSO", repository, Guid.NewGuid()));
        }
        foreach (var storage in modelBuildParams.Storages)
        {
            settings.Storages.Add(new StorageEntry(storage , "DIRECTORY", storage, Guid.NewGuid()));
        }
        var persistentState = new InternalState(settings);
        persistentState.CheckIsFirstStart();
        var model = new BSU.Core.Model.Model(persistentState, Services);
        Services.Add<IModel>(model);

        var vm = new ViewModel.ViewModel(Services);

        model.Load();

        if (modelBuildParams.SteamLoaded)
        {
            _storages["steam"].LoadEmpty();
        }

        return vm;
    }

    private IStorage CreateStorage(string url, bool isSteam)
    {
        var storage = new TestStorage(_testModelInterface, !isSteam);
        _storages.Add(url, storage);
        return storage;
    }

    private IRepository CreateRepository(string url)
    {
        var repo = new TestRepository(_testModelInterface);
        _repositories.Add(url, repo);
        return repo;
    }

    private object? HandleInteraction(ModelActionContext context)
    {
        var oldContext = _currentContext;
        _currentContext = context;
        var result = WorkLoop(context);
        _currentContext = oldContext;
        return result;
    }

    private void ModelThread(object? buildParamsObj)
    {
        try
        {
            var buildParams = (ModelBuildParams)buildParamsObj!;
            var model = BuildModel(buildParams);
            _dispatcher.Work();
            var context = new ModelActionContext(model, new TestClosable());
            _currentContext = context;
            WorkLoop(context);
        }
        catch (Exception e)
        {
            _modelThreadException = e;
            _modelThreadSuspended.Set();
        }
    }

    private object? WorkLoop(ModelActionContext context)
    {
        while (true)
        {
            if (_shutDown) return null;
            _modelThreadSuspended.Set();
            while (!_shutDown)
            {
                if (_modelThreadContinue.WaitOne(100))
                    break;
                if (_shutDown)
                    _logger.Warn("Failed to shutdown properly");
            }
            if (_shutDown)
            {
                _modelThreadSuspended.Set();
                return null;
            }
            _modelThreadAction!();
            _dispatcher.Work();
            if (context.Dialog.TryGetResult(out var dialogResult))
                return dialogResult;
        }
    }

    public void Do(Action action)
    {
        if (Thread.CurrentThread == _modelThread)
            throw new InvalidOperationException("Can only be called from the test/user thread");
        if (_modelThreadException != null) throw new InvalidOperationException("Model is in a faulted state!");
        _modelThreadAction = action;
        _modelThreadContinue.Set();
        _modelThreadSuspended.WaitOne();
        CheckException();
    }

    public void Dispose()
    {
        _shutDown = true;
        _modelThreadContinue.Set();
        _modelThread.Join();
        CheckErrorEvents();
    }

    public void CheckErrorEvents()
    {
        Assert.Empty(ErrorEvents);
        ErrorEvents.Clear();
    }

    public void WaitFor(int timeoutMs, Func<bool> condition, Func<string>? timeoutMessage = null)
    {
        var start = DateTime.Now;
        while ((DateTime.Now - start).TotalMilliseconds < timeoutMs)
        {
            if (condition()) return;
            Do(() => { });
            Thread.Sleep(1);
        }
        throw new TimeoutException(timeoutMessage?.Invoke());
    }

    public TestRepository GetRepository(string url) => _repositories[url];

    public TestStorage GetStorage(string path) => _storages[path];

    public Dialog<T> WaitForDialog<T>(int timeoutMs = 100)
    {
        var start = DateTime.Now;
        while ((DateTime.Now - start).TotalMilliseconds < timeoutMs)
        {
            if (_currentContext.Active is T active)
                return new Dialog<T>(active, _currentContext.Dialog);
            Do(() => { });
        }
        throw new TimeoutException();
    }
}

internal class Dialog<T>
{
    public T ViewModel { get; }
    public IDialogContext Closable { get; }

    public Dialog(T viewModel, IDialogContext closable)
    {
        ViewModel = viewModel;
        Closable = closable;
    }
}

internal record ModelActionContext(object Active, IDialogContext Dialog);

internal class ModelFailedException : Exception
{
    public ModelFailedException(Exception modelThreadException) : base("Model failed", modelThreadException)
    {
    }
}
