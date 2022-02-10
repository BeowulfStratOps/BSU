using System;
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

namespace BSU.Core.Tests.ActionBased;

internal class TestModel
{
    // idea: have a user thread and a model thread.
    // That way the test can be written linearly and suspend the model instead of having to deal with callbacks en masse

    private readonly AutoResetEvent _modelThreadSuspended = new(false);
    private readonly AutoResetEvent _modelThreadContinue = new(false);
    private Action<ModelActionContext>? _modelThreadAction;
    private Exception? _modelThreadException;
    private readonly TestDispatcher _dispatcher = new();

    private readonly Dictionary<string, TestRepository> _repositories = new();

    public TestModel()
    {
        var workerThread = new Thread(ModelThread);
        workerThread.Start();
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

        var interactionService = new TestInteractionService(HandleInteraction);
        services.Add<IInteractionService>(interactionService);
        services.Add<IDispatcher>(_dispatcher);
        services.Add<IEventManager>(new EventManager());
        services.Add<IRepositoryStateService>(new RepositoryStateService(services));
        var types = new Types();

        types.AddRepoType("BSO", CreateRepository);

        services.Add(types);

        var persistentState = new InternalState(new MockSettings());
        var model = new Model.Model(persistentState, services, false);
        services.Add<IModel>(model);

        var vm = new ViewModel.ViewModel(services);
        return vm;
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
        var modsDict = new Dictionary<string, TestRepositoryMod>();
        foreach (var mod in mods)
        {

        }
        _repositories[url].Load(modsDict);
    }
}

internal class TestRepositoryMod
{
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
