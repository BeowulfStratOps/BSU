using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BSU.Core.JobManager;
using BSU.Core.Model;
using BSU.Core.Persistence;
using BSU.CoreCommon;
using NLog;

[assembly: InternalsVisibleTo("BSU.Core.Tests")]
[assembly: InternalsVisibleTo("RealTest")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace BSU.Core
{
    public class Core : IDisposable
    {
        private readonly Logger _logger = EntityLogger.GetLogger();

        internal readonly IJobManager JobManager;

        internal readonly Model.Model Model;
        public readonly Types Types;
        public readonly ViewModel.ViewModel ViewModel;


        /// <summary>
        /// Create a new core instance. Should be used in a using block.
        /// </summary>
        /// <param name="settingsPath">Location to store local settings, including repo/storage data.</param>
        public Core(FileInfo settingsPath, IActionQueue dispatcher) : this(Settings.Load(settingsPath), dispatcher)
        {
        }

        internal Core(ISettings settings, IActionQueue dispatcher) : this(settings, null, dispatcher)
        {
        }


        internal Core(ISettings settings, IJobManager jobManager, IActionQueue dispatcher)
        {
            _logger.Info("Creating new core instance");
            jobManager ??= new JobManager.JobManager(dispatcher);
            JobManager = jobManager;
            Types = new Types();
            var state = new InternalState(settings);

            Model = new Model.Model(state, jobManager, Types, dispatcher);
            ViewModel = new ViewModel.ViewModel(Model);
        }

        public void Dispose() => Dispose(false);

        public void Dispose(bool blocking)
        {
            JobManager.Shutdown(blocking);
        }

        public async Task Start()
        {
            await Model.Load();
        }
    }
}
