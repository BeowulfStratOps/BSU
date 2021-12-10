using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using BSU.Core.Concurrency;
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

        /// <summary>
        /// Create a new core instance. Should be used in a using block.
        /// </summary>
        /// <param name="settingsPath">Location to store local settings, including repo/storage data.</param>
        public Core(FileInfo settingsPath) : this(Settings.Load(settingsPath))
        {
        }

        internal Core(ISettings settings)
        {
            _logger.Info("Creating new core instance");
            var state = new InternalState(settings);

            var eventBus = new SynchronizationContextEventBus(SynchronizationContext.Current!);

            _model = new Model.Model(state, Types.Default, eventBus, state.CheckIsFirstStart());
        }

        public ViewModel.ViewModel GetViewModel(IInteractionService interactionService)
        {
            return new ViewModel.ViewModel(_model, interactionService);
        }

        public void Dispose()
        {
            // TODO: cancel operations
        }
    }
}
