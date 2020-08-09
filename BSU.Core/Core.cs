using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BSU.Core.JobManager;
using BSU.Core.Model;
using BSU.Core.Persistence;
using BSU.Core.Sync;
using BSU.Core.View;
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
        public ViewModel ViewState { get; }


        /// <summary>
        /// Create a new core instance. Should be used in a using block.
        /// </summary>
        /// <param name="settingsPath">Location to store local settings, including repo/storage data.</param>
        public Core(FileInfo settingsPath, Action<Action> uiDispatcher) : this(Settings.Load(settingsPath), new JobManager.JobManager(), uiDispatcher)
        {
        }

        internal Core(ISettings settings, Action<Action> uiDispatcher) : this(settings, new JobManager.JobManager(), uiDispatcher)
        {
        }


        internal Core(ISettings settings, IJobManager jobManager, Action<Action> uiDispatcher)
        {
            Logger.Info("Creating new core instance");
            JobManager = jobManager;
            Types = new Types();
            var state = new InternalState(settings);
            Model = new Model.Model(state, jobManager, Types);
            ViewState = new ViewModel(this, uiDispatcher, Model);
        }

        public void Load()
        {
            Model.Load();
        }
            
        //CheckUpdateSettings();
        //CheckJobsWithoutUpdate();
        //CheckJobsWithoutRepositoryTarget(state);

        private void CheckUpdateSettings()
        {
            /*foreach (var storage in Model.GetStorages())
            {
                State.CleanupUpdatingTo(storage);
            }*/
        }

        private void CheckJobsWithoutUpdate()
        {
            foreach (var updateJob in JobManager.GetActiveJobs().OfType<RepoSync>())
            {
                UpdateTarget target = null; //State.GetUpdateTarget(updateJob.StorageMod);
                if (target == null)
                    throw new InvalidOperationException("There are hanging jobs. WTF.");
                if (target.Hash != updateJob.Target.Hash)
                    throw new InvalidOperationException("There are hanging jobs. WTF.");
            }
        }

        /*private void CheckJobsWithoutRepositoryTarget(State.State state)
        {
            foreach (var updateJob in JobManager.GetAllJobs().OfType<RepoSync>())
            {
                if (state.Repos.SelectMany(r => r.Mods)
                    .All(m => m.VersionHash.GetHashString() != updateJob.Target.Hash))
                    throw new InvalidOperationException("There are hanging jobs. WTF.");
            }
        }*/

        public void Dispose() => Dispose(false);

        public void Dispose(bool blocking)
        {
            // Stop all threaded operations, to ensure a graceful exit.
            Model.Shutdown();
            JobManager.Shutdown(blocking);
        }
    }
}
