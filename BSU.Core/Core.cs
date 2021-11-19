using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
            Types = new Types();
            var state = new InternalState(settings);

            Model = new Model.Model(state, Types);

            // TODO: proper service stuff?
            var eventCombiner = new StructureEventCombiner(Model);
            new AutoSelector(Model, eventCombiner);
            ViewModel = new ViewModel.ViewModel(Model, eventCombiner);
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
