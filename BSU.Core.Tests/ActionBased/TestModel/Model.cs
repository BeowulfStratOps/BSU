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
using Xunit;

namespace BSU.Core.Tests.ActionBased.TestModel;

internal class Model : IDisposable
{
    // idea: have a user thread and a model thread.
    // That way the test can be written linearly and suspend the model instead of having to deal with callbacks en masse

    private readonly Thread _modelThread;

    private bool _shutDown;

    private readonly AutoResetEvent _modelThreadSuspended = new(false);
    private readonly AutoResetEvent _modelThreadContinue = new(false);
    private Action<ModelActionContext>? _modelThreadAction;
    private Exception? _modelThreadException;
    private readonly TestDispatcher _dispatcher = new();

    private readonly Dictionary<string, TestRepository> _repositories = new();
    private readonly Dictionary<string, TestStorage> _storages = new();

    private readonly List<ErrorEvent> _errorEvents = new();
    private readonly TestModelInterface _testModelInterface;

    public Model()
    {
        _testModelInterface = new TestModelInterface(DoInModelThreadWithWait);
        _modelThread = new Thread(ModelThread);
        _modelThread.Start();
        _modelThreadSuspended.WaitOne();
        if (_modelThreadException != null)
            throw _modelThreadException;
    }

    private void DoInModelThreadWithWait(Action action, bool wait)
    {
        DoInModelThread(_ =>
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

    private object BuildModel()
    {
        var services = new ServiceProvider();

        var eventManager = new EventManager();

        var asyncVoidExecutor = new TestAsyncVoidExecutor(eventManager);

        services.Add<IAsyncVoidExecutor>(asyncVoidExecutor);
        eventManager.Subscribe<ErrorEvent>(e => _errorEvents.Add(e));

        var interactionService = new TestInteractionService(HandleInteraction);
        services.Add<IInteractionService>(interactionService);
        services.Add<IDispatcher>(_dispatcher);
        services.Add<IEventManager>(eventManager);
        services.Add<IRepositoryStateService>(new RepositoryStateService(services));
        services.Add<IDialogService>(new DialogService(services)); // TODO: we use this directly in stead of messing with the interaction service
        var types = new Types();

        types.AddRepoType("BSO", CreateRepository);
        types.AddStorageType("DIRECTORY", CreateStorage);

        services.Add(types);

        var persistentState = new InternalState(new MockSettings());
        var model = new BSU.Core.Model.Model(persistentState, services, false);
        services.Add<IModel>(model);

        var vm = new ViewModel.ViewModel(services);
        return vm;
    }

    private IStorage CreateStorage(string url)
    {
        var storage = new TestStorage(_testModelInterface);
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
        _modelThreadSuspended.Set();
        return WorkLoop(context);
    }

    private void ModelThread()
    {
        try
        {
            var model = BuildModel();
            _dispatcher.Work();
            _modelThreadSuspended.Set();
            var context = new ModelActionContext(model, new TestClosable());
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
            _modelThreadContinue.WaitOne();
            if (_shutDown) return null;
            _modelThreadAction!(context);
            _dispatcher.Work();
            if (context.Dialog.TryGetResult(out var dialogResult))
                return dialogResult;
            _modelThreadSuspended.Set();
        }
    }

    public void Do<T>(Action<T> action)
    {
        Do<T>((t, _) => action(t));
    }

    public void Do<T>(Action<T, IDialogContext> action)
    {
        DoInModelThread(context =>
        {
            var (activeObject, dialogContext) = context;
            var active = (T)activeObject;
            action(active, dialogContext);
        });
    }

    private void DoInModelThread(Action<ModelActionContext> action)
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
        Assert.Empty(_errorEvents);
        _errorEvents.Clear();
    }

    public void WaitFor(int timeoutMs, Func<bool> condition, Func<string>? timeoutMessage = null)
    {
        var start = DateTime.Now;
        while ((DateTime.Now - start).TotalMilliseconds < timeoutMs)
        {
            if (condition()) return;
            _dispatcher.Work();
            Thread.Sleep(1);
        }
        throw new TimeoutException(timeoutMessage?.Invoke());
    }

    public TestRepository GetRepository(string url) => _repositories[url];

    public TestStorage GetStorage(string path) => _storages[path];
}

internal record ModelActionContext(object Active, IDialogContext Dialog);

internal class ModelFailedException : Exception
{
    public ModelFailedException(Exception modelThreadException) : base("Model failed", modelThreadException)
    {
    }
}
