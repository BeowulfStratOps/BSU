using System;
using System.IO;
using System.Runtime.CompilerServices;
using BSU.Core.Concurrency;
using BSU.Core.Events;
using BSU.Core.Ioc;
using BSU.Core.Model;
using BSU.Core.Persistence;
using BSU.Core.Services;
using BSU.Core.ViewModel;
using BSU.CoreCommon;
using NLog;

[assembly: InternalsVisibleTo("BSU.Core.Tests")]
[assembly: InternalsVisibleTo("RealTest")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace BSU.Core
{
    public class Core : IDisposable
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly ViewModel.ViewModel _viewModel;

        /// <summary>
        /// Create a new core instance. Should be used in a using block.
        /// </summary>
        /// <param name="settingsPath">Location to store local settings, including repo/storage data.</param>
        /// <param name="interactionService"></param>
        /// <param name="dispatcher"></param>
        /// <param name="themeService"></param>
        public Core(FileInfo settingsPath, IInteractionService interactionService, IDispatcher dispatcher, IThemeService themeService) : this(Settings.Load(settingsPath), interactionService, dispatcher, themeService)
        {
        }

        internal Core(ISettings settings, IInteractionService interactionService, IDispatcher dispatcher, IThemeService themeService)
        {
            _logger.Info("Creating new core instance");
            var state = new InternalState(settings);

            EventManager = new EventManager();

            var services = new ServiceProvider();
            services.Add<IAsyncVoidExecutor>(new AsyncVoidExecutor());
            services.Add(Types.Default);
            services.Add(dispatcher);
            services.Add<IJobManager>(new JobManager(dispatcher));
            services.Add(interactionService);
            services.Add<IDialogService>(new DialogService(services));
            services.Add(EventManager);
            services.Add(themeService);
            services.Add<IModActionService>(new ModActionService());
            services.Add<IStorageService>(new StorageService());
            services.Add<IConflictService>(new ConflictService(services));
            services.Add<IErrorService>(new ErrorService(services));
            services.Add<IAutoSelectionService>(new AutoSelectionService(services));
            services.Add<IRepositoryStateService>(new RepositoryStateService(services));
            
            var model = new Model.Model(state, services);
            services.Add<IModel>(model);

            // TODO: build service provider from registrations 
            
            // TODO: should this be registered somewhere? - only if we want to stop them to cleanly dispose things.
            new PresetGeneratorActor(services);
            new BiKeyCopyActor(services);
            new AutoSelectionActor(services);
            new EventCombineActor(services);

            _viewModel = new ViewModel.ViewModel(services);

            SetStartingTheme(settings, EventManager, themeService);

            model.Load();
        }

        public IEventManager EventManager { get; }

        private void SetStartingTheme(ISettings settings, IEventManager eventManager, IThemeService themeService)
        {
            if (settings.GlobalSettings.Theme == null)
            {
                var defaultTheme = themeService.GetDefaultTheme();
                settings.GlobalSettings.Theme = defaultTheme;
                settings.Store();
                themeService.SetTheme(defaultTheme);
                eventManager.Publish(new NotificationEvent(
                    $"Selected theme '{defaultTheme}'. Can be changed in settings."));
            }
            themeService.SetTheme(settings.GlobalSettings.Theme!);
        }

        public ViewModel.ViewModel GetViewModel() => _viewModel;

        public void Dispose()
        {
            // TODO: cancel operations
        }
    }
}
