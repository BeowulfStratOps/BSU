using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BSU.Core.JobManager;
using BSU.Core.Services;
using BSU.Core.State;
using BSU.Core.Sync;
using BSU.CoreCommon;
using NLog;
using DownloadAction = BSU.Core.State.DownloadAction;
using Repository = BSU.Core.State.Repository;
using UpdateAction = BSU.Core.State.UpdateAction;

[assembly: InternalsVisibleTo("BSU.Core.Tests")]
[assembly: InternalsVisibleTo("RealTest")]

namespace BSU.Core
{
    public class Core : IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        internal readonly InternalState State;
        internal readonly IJobManager JobManager;

        internal Core(ISettings settings, IJobManager jobManager)
        {
            Logger.Info("Creating new core instance");
            JobManager = jobManager;
            State = new InternalState(settings);
        }

        /// <summary>
        /// Create a new core instance. Should be used in a using block.
        /// </summary>
        /// <param name="settingsPath">Location to store local settings, including repo/storage data.</param>
        public Core(FileInfo settingsPath) : this(Settings.Load(settingsPath), ServiceProvider.JobManager)
        {
        }

        internal Core(ISettings settings) : this(settings, ServiceProvider.JobManager)
        {
        }

        public void AddRepoType(string name, Func<string, string, IRepository> create) =>
            State.AddRepoType(name, create);

        public IEnumerable<string> GetRepoTypes() => State.GetRepoTypes();

        public void AddStorageType(string name, Func<string, string, IStorage> create) =>
            State.AddStorageType(name, create);

        public IEnumerable<string> GetStorageTypes() => State.GetStorageTypes();

        /// <summary>
        /// Adds a repository of given type.
        /// </summary>
        /// <param name="name">Identifier for this repository</param>
        /// <param name="url">Url of the repository file</param>
        /// <param name="type">Types, as contained in repo-types</param>
        public void AddRepo(string name, string url, string type) => State.AddRepo(name, url, type);

        /// <summary>
        /// Removes a repository.
        /// </summary>
        /// <param name="repo">Repository identifier.</param>
        /// <exception cref="InvalidOperationException">Fails if the repository has running jobs</exception>
        internal void RemoveRepo(Repository repo)
        {
            if (repo.BackingRepository.GetMods().Any(mod => GetActiveJobs(mod).Any()))
            {
                throw new InvalidOperationException("Can't remove repository while it has jobs running!");
            }

            State.RemoveRepo(repo.BackingRepository);
        }

        /// <summary>
        /// Removes a storage
        /// </summary>
        /// <param name="storage">Storage identifier.</param>
        /// <exception cref="InvalidOperationException">Fails if the storage has running jobs.</exception>
        public void RemoveStorage(State.Storage storage)
        {
            if (storage.BackingStorage.GetMods().Any(mod => GetActiveJob(mod) != null))
            {
                throw new InvalidOperationException("Can't remove storage while it has jobs running!");
            }

            State.RemoveStorage(storage.BackingStorage);
        }

        /// <summary>
        /// Adds a storage of the given type.
        /// </summary>
        /// <param name="name">Identifier.</param>
        /// <param name="directory">Directory on the local file system.</param>
        /// <param name="type">Types, as listed in the storage types.</param>
        public void AddStorage(string name, DirectoryInfo directory, string type) =>
            State.AddStorage(name, directory, type);

        /// <summary>
        /// Calculated a time-slice state of all repositories, storages, and their mods.
        /// Does all the hard work atm, should be run async.
        /// </summary>
        /// <returns></returns>
        public State.State GetState()
        {
            CheckUpdateSettings();
            var state = new State.State(State.GetRepositories(), State.GetStorages(), this);
            CheckJobsWithoutUpdate();
            CheckJobsWithoutRepositoryTarget(state);
            return state;
        }

        private void CheckUpdateSettings()
        {
            foreach (var storage in State.GetStorages())
            {
                State.CleanupUpdatingTo(storage);
            }
        }

        private void CheckJobsWithoutUpdate()
        {
            foreach (var updateJob in JobManager.GetActiveJobs().OfType<RepoSync>())
            {
                var target = State.GetUpdateTarget(updateJob.StorageMod);
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

        internal UpdatePacket PrepareUpdate(Repository repo, State.State state)
        {
            Logger.Debug("Preparing update");
            var todos = repo.Mods.Where(m => m.Selected != null && !(m.Selected is UseAction)).ToList();

            var actions = todos.Select(m => m.Selected).ToList();

            var updatePacket = new UpdatePacket(this, state);

            foreach (var downloadAction in actions.OfType<DownloadAction>())
            {
                var storageMod = downloadAction.Storage.BackingStorage.CreateMod(downloadAction.FolderName);
                updatePacket.Rollback.Add(() =>
                    downloadAction.Storage.BackingStorage.RemoveMod(downloadAction.FolderName));
                var syncState = new RepoSync(downloadAction.RepositoryMod.Mod, storageMod, downloadAction.UpdateTarget);
                updatePacket.Jobs.Add(syncState);
            }


            foreach (var updateAction in actions.OfType<UpdateAction>())
            {
                var syncState = new RepoSync(updateAction.RepositoryMod.Mod, updateAction.StorageMod.Mod,
                    updateAction.UpdateTarget);
                updatePacket.Jobs.Add(syncState);
            }

            return updatePacket;
        }

        internal void DoUpdate(UpdatePacket update)
        {
            Logger.Debug("Doing update");
            InvalidateState();
            foreach (var job in update.Jobs)
            {
                if (!(job is RepoSync sync)) throw new InvalidCastException("WTF..");
                State.SetUpdatingTo(sync.StorageMod, sync.Target.Hash, sync.Target.Display);
                // TODO: do some sanity checks. two update jobs must never have the same storage mod
                job.JobEnded += s => { InvalidateState(); };
                JobManager.QueueJob(sync);
            }
        }

        /// <summary>
        /// Print the internal state - for debugging.
        /// </summary>
        public void PrintInternalState() => State.PrintState();

        internal UpdateTarget GetUpdateTarget(StorageMod mod) => State.GetUpdateTarget(mod.Mod);

        internal void UpdateDone(IStorageMod mod) => State.RemoveUpdatingTo(mod);

        /// <summary>
        /// Get all jobs the JobManager is aware of.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IJobFacade> GetAllJobs() => JobManager.GetAllJobs().OfType<IJobFacade>(); // TODO: other jobs?

        /// <summary>
        /// Get all jobs that are currently running or queued.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IJobFacade> GetActiveJobs() => JobManager.GetActiveJobs().OfType<IJobFacade>(); // TODO: other jobs?

        internal RepoSync GetActiveJob(IStorageMod mod)
        {
            return JobManager.GetActiveJobs().OfType<RepoSync>().SingleOrDefault(j => j.StorageMod == mod);
        }

        internal IEnumerable<RepoSync> GetActiveJobs(IRepositoryMod mod)
        {
            return JobManager.GetActiveJobs().OfType<RepoSync>().Where(j => j.RepositoryMod == mod);
        }

        public void Dispose() => Dispose(false);

        public void Dispose(bool blocking)
        {
            // Stop all threaded operations, to ensure a graceful exit.
            JobManager.Shutdown(blocking);
        }

        internal event Action StateInvalidated;
        private void InvalidateState() => StateInvalidated?.Invoke();
    }
}
