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
        public Core(FileInfo settingsPath, IInteractionService interactionService, IDispatcher dispatcher) : this(Settings.Load(settingsPath), interactionService, dispatcher)
        {
        }

        internal Core(ISettings settings, IInteractionService interactionService, IDispatcher dispatcher)
        {
            _logger.Info("Creating new core instance");
            var state = new InternalState(settings);

            var services = new ServiceProvider();
            services.Add<IAsyncVoidExecutor>(new AsyncVoidExecutor());
            services.Add(Types.Default);
            services.Add(dispatcher);
            services.Add(interactionService);
            services.Add<IDialogService>(new DialogService(services));
            services.Add<IEventManager>(new EventManager());
            services.Add<IRepositoryStateService>(new RepositoryStateService(services));

            // TODO: should this be registered somewhere?
            new PresetGeneratorService(services);

            var model = new Model.Model(state, services, state.CheckIsFirstStart());
            services.Add<IModel>(model);

            // TODO: should we use different service providers to avoid accidental abuse?
            _viewModel = new ViewModel.ViewModel(services);
            model.Load();
        }

        public ViewModel.ViewModel GetViewModel() => _viewModel;

        public void Dispose()
        {
            // TODO: cancel operations
        }
    }
}
