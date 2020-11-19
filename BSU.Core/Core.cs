using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BSU.Core.JobManager;
using BSU.Core.Persistence;
using NLog;

[assembly: InternalsVisibleTo("BSU.Core.Tests")]
[assembly: InternalsVisibleTo("RealTest")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace BSU.Core
{
    public class Core : IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        internal readonly IJobManager JobManager;
        
        internal readonly Model.Model Model;
        public readonly Types Types;
        public readonly ViewModel.ViewModel ViewModel;


        /// <summary>
        /// Create a new core instance. Should be used in a using block.
        /// </summary>
        /// <param name="settingsPath">Location to store local settings, including repo/storage data.</param>
        public Core(FileInfo settingsPath, Action<Action> dispatch) : this(Settings.Load(settingsPath), dispatch)
        {
        }

        internal Core(ISettings settings, Action<Action> dispatch) : this(settings, null, dispatch)
        {
        }


        internal Core(ISettings settings, IJobManager jobManager, Action<Action> dispatch)
        {
            Logger.Info("Creating new core instance");
            var dispatcher = new Dispatcher(dispatch);
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
