using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Events;
using BSU.Core.Ioc;
using BSU.Core.Model;
using BSU.Core.Persistence;
using BSU.Core.Services;
using BSU.Core.Tests.Mocks;
using BSU.Core.ViewModel;
using BSU.Core.ViewModel.Util;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Tests.ActionBased;

internal class TestModel : IDisposable
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

    public TestModel()
    {
        _modelThread = new Thread(ModelThread);
        _modelThread.Start();
        _modelThreadSuspended.WaitOne();
        if (_modelThreadException != null)
            throw _modelThreadException;
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
        var model = new Model.Model(persistentState, services, false);
        services.Add<IModel>(model);

        var vm = new ViewModel.ViewModel(services);
        return vm;
    }

    private IStorage CreateStorage(string url)
    {
        var storage = new TestStorage();
        _storages.Add(url, storage);
        return storage;
    }

    private IRepository CreateRepository(string url)
    {
        var repo = new TestRepository();
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

    public void Close(bool result)
    {
        DoInModelThread(context => context.Dialog.SetResult(result));
    }

    private void DoInModelThread(Action<ModelActionContext> action)
    {
        if (_modelThreadException != null) throw new InvalidOperationException("Model is in a faulted state!");
        _modelThreadAction = action;
        _modelThreadContinue.Set();
        _modelThreadSuspended.WaitOne();
        CheckException();
    }

    public void LoadRepository(string url, IEnumerable<string> mods)
    {
        if (Thread.CurrentThread != _modelThread) throw new InvalidOperationException();
        var modsDict = new Dictionary<string, IRepositoryMod>();
        foreach (var modName in mods)
        {
            var mod = new TestRepositoryMod();
            modsDict.Add(modName, mod);
        }
        _repositories[url].Load(modsDict);
        _dispatcher.WaitForWork();
        _dispatcher.Work();
    }

    public void LoadRepositoryMod(string repoUrl, string modName, Dictionary<string,byte[]> files)
    {
        if (Thread.CurrentThread != _modelThread) throw new InvalidOperationException();
        var mod = _repositories[repoUrl].GetMod(modName);
        mod.Load(files);
        _dispatcher.WaitForWork();
        _dispatcher.Work();
    }

    public void Dispose()
    {
        _shutDown = true;
        _modelThreadContinue.Set();
        _modelThread.Join();
    }

    public void LoadStorage(string path, IEnumerable<string> mods)
    {
        if (Thread.CurrentThread != _modelThread) throw new InvalidOperationException();
        var modsDict = new Dictionary<string, IStorageMod>();
        foreach (var modName in mods)
        {
            var mod = new TestStorageMod();
            modsDict.Add(modName, mod);
        }
        _storages[path].Load(modsDict);
        _dispatcher.WaitForWork();
        _dispatcher.Work();
    }

    public List<ErrorEvent> GetErrorEvents()
    {
        var result = new List<ErrorEvent>(_errorEvents);
        _errorEvents.Clear();
        return result;
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

    public void LoadStorageMod(string path, string modName, Dictionary<string, byte[]> files, bool waitForStateChange = true)
    {
        if (Thread.CurrentThread != _modelThread) throw new InvalidOperationException();
        var mod = _storages[path].GetMod(modName);
        mod.Load(files);
        if (!waitForStateChange) return;
        _dispatcher.WaitForWork();
        _dispatcher.Work();
    }

    public void FinishUpdate(string repoUrl, string modName)
    {
        if (Thread.CurrentThread != _modelThread) throw new InvalidOperationException();
        _repositories[repoUrl].GetMod(modName).FinishUpdate();
    }

    public Dictionary<string, byte[]> GetRepoFiles(string url, string modName)
    {
        return _repositories[url].GetMod(modName).Files;
    }

    public Dictionary<string, byte[]> GetStorageFiles(string url, string modName)
    {
        return _storages[url].GetMod(modName).Files;
    }
}

internal class TestAsyncVoidExecutor : IAsyncVoidExecutor
{
    private readonly IEventManager _eventManager;
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    public TestAsyncVoidExecutor(IEventManager eventManager)
    {
        _eventManager = eventManager;
    }

    public async void Execute(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception e)
        {
            Logger.Error(e);
            _eventManager.Publish(new ErrorEvent(e.Message));
            throw;
        }
    }
}

internal record ModelActionContext(object Active, IDialogContext Dialog);

internal interface IDialogContext
{
    void SetResult(object? result);
    bool TryGetResult(out object? result);
}

internal class DialogContext : IDialogContext
{
    private object? _result;
    private bool _isSet;

    public void SetResult(object? result)
    {
        _result = result;
        _isSet = true;
    }

    public bool TryGetResult(out object? result)
    {
        result = _result;
        return _isSet;
    }
}

internal class ModelFailedException : Exception
{
    public ModelFailedException(Exception modelThreadException) : base("Model failed", modelThreadException)
    {

    }
}

internal class TestClosable : DialogContext, ICloseable
{
    public void Close(bool result)
    {
        SetResult(result);
    }
}
