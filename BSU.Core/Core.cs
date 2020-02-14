using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BSU.Core.Hashes;
using BSU.Core.JobManager;
using BSU.Core.Model;
using BSU.Core.Services;
using BSU.Core.State;
using BSU.Core.Sync;
using BSU.Core.View;
using BSU.CoreCommon;
using NLog;
using DownloadAction = BSU.Core.State.DownloadAction;
using Repository = BSU.Core.State.Repository;
using StorageMod = BSU.Core.State.StorageMod;
using UpdateAction = BSU.Core.State.UpdateAction;

[assembly: InternalsVisibleTo("BSU.Core.Tests")]
[assembly: InternalsVisibleTo("RealTest")]

namespace BSU.Core
{
    public class Core : IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        internal readonly IJobManager JobManager;
        private readonly Action<Action> _uiDispatcher;
        
        private readonly Model.Model Model;
        public ViewModel ViewState { get; }


        /// <summary>
        /// Create a new core instance. Should be used in a using block.
        /// </summary>
        /// <param name="settingsPath">Location to store local settings, including repo/storage data.</param>
        public Core(FileInfo settingsPath, Action<Action> uiDispatcher) : this(Settings.Load(settingsPath), ServiceProvider.JobManager, uiDispatcher)
        {
        }

        internal Core(ISettings settings, Action<Action> uiDispatcher) : this(settings, ServiceProvider.JobManager, uiDispatcher)
        {
        }


        internal Core(ISettings settings, IJobManager jobManager, Action<Action> uiDispatcher)
        {
            Logger.Info("Creating new core instance");
            JobManager = jobManager;
            _uiDispatcher = uiDispatcher;
            var types = new Types();
            var state = new InternalState(settings, types);
            ServiceProvider.InternalState = state;
            Model = new Model.Model(state);
            ViewState = new ViewModel(this, uiDispatcher, Model);
        }

        public void Load()
        {
            Model.Load();
        }

        /// <summary>
        /// Calculated a time-slice state of all repositories, storages, and their mods.
        /// Does all the hard work atm, should be run async.
        /// </summary>
        /// <returns></returns>
        public State.State GetState()
        {
            CheckUpdateSettings();
            State.State state = null; //new State.State(State.GetRepositories(), State.GetStorages(), this);
            CheckJobsWithoutUpdate();
            CheckJobsWithoutRepositoryTarget(state);
            return state;
        }

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

        private void CheckJobsWithoutRepositoryTarget(State.State state)
        {
            foreach (var updateJob in JobManager.GetAllJobs().OfType<RepoSync>())
            {
                if (state.Repos.SelectMany(r => r.Mods)
                    .All(m => m.VersionHash.GetHashString() != updateJob.Target.Hash))
                    throw new InvalidOperationException("There are hanging jobs. WTF.");
            }
        }

        public void Dispose() => Dispose(false);

        public void Dispose(bool blocking)
        {
            // Stop all threaded operations, to ensure a graceful exit.
            JobManager.Shutdown(blocking);
        }
    }
}
