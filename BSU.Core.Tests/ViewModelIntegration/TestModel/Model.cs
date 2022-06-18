using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Events;
using BSU.Core.Ioc;
using BSU.Core.Model;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.Core.Services;
using BSU.Core.ViewModel;
using BSU.Core.ViewModel.Util;
using BSU.CoreCommon;
using Xunit.Abstractions;

namespace BSU.Core.Tests.ViewModelIntegration.TestModel;

internal class Model : IAsyncDisposable
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly TestDispatcher _dispatcher = new();

    private readonly Dictionary<string, TestRepository> _repositories = new();
    private readonly Dictionary<string, TestStorage> _storages = new();

    public readonly List<ErrorEvent> ErrorEvents = new();

    public readonly ServiceProvider Services = new();
    private readonly ViewModel.ViewModel _model;
    private readonly TestInteractionService _interactionService;
    private readonly TestAsyncVoidExecutor _asyncVoidExecutor;
    private readonly DeterministicJobManager _jobManager;

    public Model(ITestOutputHelper outputHelper, IEnumerable<string>? repositories = null, IEnumerable<string>? storages = null)
    {
        _outputHelper = outputHelper;
        var eventManager = new EventManager();

        _asyncVoidExecutor = new TestAsyncVoidExecutor(eventManager);

        _interactionService = new TestInteractionService();

        Services.Add<IAsyncVoidExecutor>(_asyncVoidExecutor);
        eventManager.Subscribe<ErrorEvent>(e => ErrorEvents.Add(e));

        Services.Add<IInteractionService>(_interactionService);
        Services.Add<IDispatcher>(_dispatcher);
        _jobManager = new DeterministicJobManager(_asyncVoidExecutor);
        Services.Add<IJobManager>(_jobManager);
        Services.Add<IUpdateService>(new UpdateService(Services));
        Services.Add<IEventManager>(eventManager);
        Services.Add<IModActionService>(new ModActionService());
        Services.Add<IStorageService>(new StorageService());
        Services.Add<IConflictService>(new ConflictService(Services));
        Services.Add<IAutoSelectionService>(new AutoSelectionService(Services));
        Services.Add<IErrorService>(new ErrorService(Services));
        Services.Add<IRepositoryStateService>(new RepositoryStateService(Services));
        Services.Add<IDialogService>(new DialogService(Services));
        var types = new Types();

        types.AddRepoType("BSO", (u, _) => CreateRepository(u));
        types.AddStorageType("DIRECTORY", (p, _) => CreateStorage(p, false));
        types.AddStorageType("STEAM", (p, _) => CreateStorage(p, true));

        Services.Add(types);

        var settings = new MockSettings();
        foreach (var repository in repositories ?? new List<string>())
        {
            settings.Repositories.Add(new RepositoryEntry(repository, "BSO", repository, Guid.NewGuid()));
        }
        foreach (var storage in storages ?? new List<string>())
        {
            settings.Storages.Add(new StorageEntry(storage , "DIRECTORY", storage, Guid.NewGuid()));
        }
        var persistentState = new InternalState(settings);
        persistentState.CheckIsFirstStart();
        var model = new BSU.Core.Model.Model(persistentState, Services);
        Services.Add<IModel>(model);
        
        //new PresetGeneratorActor(Services);
        //new BiKeyCopyActor(Services);
        new AutoSelectionActor(Services);
        new EventCombineActor(Services);

        var vm = new ViewModel.ViewModel(Services);
        

        model.Load();

        _interactionService.SetViewModel(vm);

        _model = vm;
    }

    private IStorage CreateStorage(string url, bool isSteam)
    {
        var storage = new TestStorage(!isSteam);
        _storages.Add(url, storage);
        return storage;
    }

    private IRepository CreateRepository(string url)
    {
        var repo = new TestRepository();
        _repositories.Add(url, repo);
        return repo;
    }

    public TestRepository GetRepository(string url) => _repositories[url];

    public TestStorage GetStorage(string path) => _storages[path];

    public async Task<Dialog<T>> WaitForDialog<T>()
    {
        for (int i = 0; i < TaskYieldLimit; i++)
        {
            if (_interactionService.GetCurrentDialog() is Dialog<T> dialog)
                return dialog;
            await Task.Yield();
        }

        throw new TaskYieldLimitReachedException();
    }

    private const int TaskYieldLimit = 100;

    public async ValueTask DisposeAsync()
    {
        var jobs = _jobManager.GetRunningJobs();
        if (jobs.Any())
            _outputHelper.WriteLine($"There are still running jobs: {string.Join(", ", jobs)}");
        if (!await _asyncVoidExecutor.WaitForRunningTasks())
            _outputHelper.WriteLine($"Unfinished tasks in the AsyncVoidExecutor");
    }
}

internal class TaskYieldLimitReachedException : Exception
{
}

internal class Dialog<TViewModel> : IDialog
{
    public TViewModel ViewModel { get; }
    private readonly TaskCompletionSource<object?> _tcs;

    public ICloseable Closable => new ClosableTask(r => _tcs.SetResult(r));

    public Dialog(TViewModel viewModel, TaskCompletionSource<object?> tcs)
    {
        ViewModel = viewModel;
        _tcs = tcs;
    }

    private class ClosableTask : ICloseable
    {
        private readonly Action<bool> _close;

        public ClosableTask(Action<bool> close)
        {
            _close = close;
        }

        public void Close(bool result) => _close(result);
    }
}

internal interface IDialog
{
}
