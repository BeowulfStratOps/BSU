using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using BSU.Core.Concurrency;
using BSU.Core.Events;
using BSU.Core.Ioc;
using BSU.Core.Persistence;
using BSU.Core.ViewModel;
using NLog;

[assembly: InternalsVisibleTo("BSU.Core.Tests")]
[assembly: InternalsVisibleTo("RealTest")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace BSU.Core
{
    public class Core : IDisposable
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly Model.Model _model;
        private readonly ViewModel.ViewModel _viewModel;

        /// <summary>
        /// Create a new core instance. Should be used in a using block.
        /// </summary>
        /// <param name="settingsPath">Location to store local settings, including repo/storage data.</param>
        /// <param name="interactionService"></param>
        public Core(FileInfo settingsPath, IInteractionService interactionService) : this(Settings.Load(settingsPath), interactionService)
        {
        }

        internal Core(ISettings settings, IInteractionService interactionService)
        {
            _logger.Info("Creating new core instance");
            var state = new InternalState(settings);

            var eventBus = new SynchronizationContextDispatcher(SynchronizationContext.Current!);

            var services = new ServiceProvider();
            services.Add(Types.Default);
            services.Add<IDispatcher>(eventBus);
            services.Add(interactionService);
            services.Add<IDialogService>(new DialogService(services));
            services.Add<IEventManager>(new EventManager());

            // TODO: both model and view model add them selves to the services. that seems wrong.
            _model = new Model.Model(state, services, state.CheckIsFirstStart());

            // TODO: should we use different service providers to avoid accidental abuse?
            _viewModel = new ViewModel.ViewModel(services);
            _model.Load();
        }

        public ViewModel.ViewModel GetViewModel() => _viewModel;

        public void Dispose()
        {
            // TODO: cancel operations
        }
    }
}
