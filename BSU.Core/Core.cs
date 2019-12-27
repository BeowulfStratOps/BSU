using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BSU.Core.Hashes;
using BSU.Core.JobManager;
using BSU.Core.State;
using BSU.Core.Sync;
using BSU.CoreCommon;
using NLog;
using DownloadAction = BSU.Core.State.DownloadAction;
using UpdateAction = BSU.Core.State.UpdateAction;

[assembly: InternalsVisibleTo("BSU.Core.Tests")]
[assembly: InternalsVisibleTo("RealTest")]

namespace BSU.Core
{
    public class Core
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        internal readonly InternalState State;
        internal readonly IJobManager<RepoSync> SyncManager;

        internal Core(ISettings settings, IJobManager<RepoSync> syncManager)
        {
            Logger.Info("Creating new core instance");
            SyncManager = syncManager;
            State = new InternalState(settings);
        }

        public Core(FileInfo settingsPath) : this(Settings.Load(settingsPath), new JobManager<RepoSync>())
        {
        }

        public Core(ISettings settings) : this(settings, new JobManager<RepoSync>())
        {
        }

        public void AddRepoType(string name, Func<string, string, IRepository> create) => State.AddRepoType(name, create);
        public IEnumerable<string> GetRepoTypes() => State.GetRepoTypes();
        public void AddStorageType(string name, Func<string, string, IStorage> create) => State.AddStorageType(name, create);
        public IEnumerable<string> GetStorageTypes() => State.GetStorageTypes();

        public void AddRepo(string name, string url, string type) => State.AddRepo(name, url, type);
        internal void RemoveRepo(Repository repo)
        {
            if (repo.BackingRepository.GetMods().Any(mod => GetActiveJobs(mod).Any()))
            {
                throw new InvalidOperationException("Can't remove repository while it has jobs running!");
            }

            State.RemoveRepo(repo.BackingRepository);
        }

        public void RemoveStorage(State.Storage storage)
        {
            if (storage.BackingStorage.GetMods().Any(mod => GetActiveJob(mod) != null))
            {
                throw new InvalidOperationException("Can't remove storage while it has jobs running!");
            }

            State.RemoveStorage(storage.BackingStorage);
        }

        public void AddStorage(string name, DirectoryInfo directory, string type) =>
            State.AddStorage(name, directory, type);

        /// <summary>
        /// Does all the hard work. Don't spam it.
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
            foreach (var updateJob in SyncManager.GetActiveJobs())
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
            foreach (var updateJob in SyncManager.GetAllJobs())
            {
                if (state.Repos.SelectMany(r => r.Mods).All(m => m.VersionHash.GetHashString() != updateJob.Target.Hash))
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
                updatePacket.Rollback.Add(() => downloadAction.Storage.BackingStorage.RemoveMod(downloadAction.FolderName));
                var syncState = new RepoSync(downloadAction.RepositoryMod.Mod, storageMod, downloadAction.UpdateTarget);
                updatePacket.Jobs.Add(syncState);
            }


            foreach (var updateAction in actions.OfType<UpdateAction>())
            {
                var syncState = new RepoSync(updateAction.RepositoryMod.Mod, updateAction.StorageMod.Mod, updateAction.UpdateTarget);
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
                SyncManager.QueueJob(sync);
            }
        }

        public void PrintInternalState() => State.PrintState();

        public UpdateTarget GetUpdateTarget(StorageMod mod) => State.GetUpdateTarget(mod.Mod);

        internal void UpdateDone(IStorageMod mod) => State.RemoveUpdatingTo(mod);

        public IEnumerable<IJobFacade> GetAllJobs() => SyncManager.GetAllJobs();
        public IEnumerable<IJobFacade> GetActiveJobs() => SyncManager.GetActiveJobs();

        internal RepoSync GetActiveJob(IStorageMod mod)
        {
            return SyncManager.GetActiveJobs().SingleOrDefault(j => j.StorageMod == mod);
        }

        internal IEnumerable<RepoSync> GetActiveJobs(IRepositoryMod mod)
        {
            return SyncManager.GetActiveJobs().Where(j => j.RepositoryMod == mod);
        }

        public void Shutdown()
        {
            SyncManager.Shutdown();
        }

        internal event Action StateInvalidated;
        internal void InvalidateState() => StateInvalidated?.Invoke();
    }
}
