using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Model;
using BSU.Core.Persistence;
using BSU.Core.Services;
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

        internal readonly Model.Model Model;
        public readonly Types Types;
        public readonly ViewModel.ViewModel ViewModel;


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

            var eventBus = new SynchronizationContextEventBus(SynchronizationContext.Current);

            Model = new Model.Model(state, Types.Default, eventBus, state.CheckIsFirstStart());
            ViewModel = new ViewModel.ViewModel(Model);
        }

        public void Dispose()
        {
            // TODO: cancel operations
        }

        public async Task Load()
        {
            Model.Load();
        }
    }
}
