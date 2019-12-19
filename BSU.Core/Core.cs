using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BSU.Core.Hashes;
using BSU.Core.State;
using BSU.Core.Sync;
using BSU.CoreInterface;
using DownloadAction = BSU.Core.State.DownloadAction;
using UpdateAction = BSU.Core.State.UpdateAction;

[assembly: InternalsVisibleTo("BSU.Core.Tests")]

namespace BSU.Core
{
    public class Core
    {
        internal readonly InternalState State;
        internal readonly ISyncManager SyncManager;

        internal Core(ISettings settings, ISyncManager syncManager)
        {
            SyncManager = syncManager;
            State = new InternalState(settings);
        }

        public Core(FileInfo settingsPath) : this(Settings.Load(settingsPath), new SyncManager())
        {
        }

        public Core(ISettings settings) : this(settings, new SyncManager())
        {
        }

        public void AddRepoType(string name, Func<string, string, IRepository> create) => State.AddRepoType(name, create);
        public List<string> GetRepoTypes() => State.GetRepoTypes();
        public void AddStorageType(string name, Func<string, string, IStorage> create) => State.AddStorageType(name, create);
        public List<string> GetStorageTypes() => State.GetStorageTypes();

        public void AddRepo(string name, string url, string type) => State.AddRepo(name, url, type);
        public void RemoveRepo(string name) => State.RemoveRepo(name);
        public void RemoveStorage(string name) => State.RemoveStorage(name);

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
            CheckJobsWithoutRemoteTarget(state);
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
                var target = State.GetUpdateTarget(updateJob.LocalMod);
                if (target == null)
                    throw new InvalidOperationException("There are hanging jobs. WTF.");
                if (target.Hash != updateJob.Target.Hash)
                    throw new InvalidOperationException("There are hanging jobs. WTF.");
            }
        }

        private void CheckJobsWithoutRemoteTarget(State.State state)
        {
            foreach (var updateJob in SyncManager.GetAllJobs())
            {
                if (state.Repos.SelectMany(r => r.Mods).All(m => m.VersionHash.GetHashString() != updateJob.Target.Hash))
                    throw new InvalidOperationException("There are hanging jobs. WTF.");
            }
        }

        internal UpdatePacket PrepareUpdate(Repo repo)
        {
            var todos = repo.Mods.Where(m => m.Selected != null && !(m.Selected is UseAction)).ToList();

            var actions = todos.Select(m => m.Selected).ToList();

            // TODO: make sure download folder names don't overlap

            var updatePacket = new UpdatePacket(this);

            foreach (var downloadAction in actions.OfType<DownloadAction>())
            {
                var localMod = downloadAction.Storage.BackingStorage.CreateMod(downloadAction.FolderName);
                var syncState = new RepoSync(downloadAction.RemoteMod.Mod, localMod);
                var updateJob = new UpdateJob(localMod, downloadAction.RemoteMod.Mod, downloadAction.UpdateTarget, syncState);
                updatePacket.Jobs.Add(updateJob);
            }


            foreach (var updateAction in actions.OfType<UpdateAction>())
            {
                var syncState = new RepoSync(updateAction.RemoteMod.Mod, updateAction.LocalMod.Mod);
                var updateJob = new UpdateJob(updateAction.LocalMod.Mod, updateAction.RemoteMod.Mod, updateAction.UpdateTarget, syncState);
                updatePacket.Jobs.Add(updateJob);
            }

            return updatePacket;
        }

        internal void DoUpdate(UpdatePacket update)
        {
            foreach (var job in update.Jobs)
            {
                State.SetUpdatingTo(job.LocalMod, job.Target.Hash, job.Target.Display);
                // TODO: do some sanity checks. two update jobs must never have the same local mod
                SyncManager.QueueJob(job);
            }
        }

        public void PrintInternalState() => State.PrintState();

        public UpdateTarget GetUpdateTarget(StorageMod mod) => State.GetUpdateTarget(mod.Mod);

        internal void UpdateDone(ILocalMod mod) => State.RemoveUpdatingTo(mod);

        public List<JobView> GetAllJobs() => SyncManager.GetAllJobs().Select(j => new JobView(j)).ToList();
        public List<JobView> GetActiveJobs() => SyncManager.GetActiveJobs().Select(j => new JobView(j)).ToList();

        internal UpdateJob GetActiveJob(ILocalMod mod)
        {
            return SyncManager.GetActiveJobs().SingleOrDefault(j => j.LocalMod == mod);
        }

        internal event Action StateInvalidated;
        internal void InvalidateState() => StateInvalidated?.Invoke();
    }
}
