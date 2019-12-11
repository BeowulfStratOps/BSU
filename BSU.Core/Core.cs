using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BSU.Core.Hashes;
using BSU.Core.State;
using BSU.Core.Sync;
using BSU.CoreInterface;
using UpdateAction = BSU.Core.State.UpdateAction;

[assembly: InternalsVisibleTo("BSU.Core.Tests")]

namespace BSU.Core
{
    public class Core
    {
        private readonly InternalState _state;
        internal readonly SyncManager SyncManager = new SyncManager();

        public Core(FileInfo settingsPath)
        {
            _state = new InternalState(Settings.Load(settingsPath));
        }

        public Core(ISettings settings)
        {
            _state = new InternalState(settings);
        }

        public void AddRepoType(string name, Func<string, string, IRepository> create) => _state.AddRepoType(name, create);
        public void AddStorageType(string name, Func<string, string, IStorage> create) => _state.AddStorageType(name, create);

        public void AddRepo(string name, string url, string type) => _state.AddRepo(name, url, type);

        public void AddStorage(string name, DirectoryInfo directory, string type) =>
            _state.AddStorage(name, directory, type);

        /// <summary>
        /// Does all the hard work. Don't spam it.
        /// </summary>
        /// <returns></returns>
        public State.State GetState()
        {
            CheckUpdateSettings();
            CheckJobsWithoutUpdate();
            return new State.State(_state.GetRepositories(), _state.GetStorages(), this);
        }

        private void CheckUpdateSettings()
        {
            foreach (var storage in _state.GetStorages())
            {
                _state.CleanupUpdatingTo(storage);
            }
        }

        private void CheckJobsWithoutUpdate()
        {
            foreach (var updateJob in SyncManager.GetAllJobs())
            {
                if (_state.GetUpdateTarget(updateJob.LocalMod) == null)
                    throw new InvalidOperationException("There are hanging jobs. WTF.");
            }
        }

        internal UpdatePacket PrepareUpdate(Repo repo)
        {
            Console.WriteLine("To do:");
            var todos = repo.Mods.Where(m => !(m.Selected is UseAction)).ToList();
            foreach (var repoModView in todos)
            {
                Console.WriteLine(repoModView.Name + ": " + repoModView.Selected.ToString());
            }

            // TODO: make sure download folder names don't overlap

            var updatePacket = new UpdatePacket(this);

            // TODO: create download folders and add them to syncstates

            foreach (var updateAction in repo.Mods.Select(m => m.Selected).OfType<UpdateAction>())
            {
                var syncState = new RepoSync(updateAction.RemoteMod.Mod, updateAction.LocalMod.Mod);
                var updateJob = new UpdateJob(updateAction.LocalMod.Mod, updateAction.RemoteMod.Mod, updateAction.Target, syncState);
                updatePacket.Jobs.Add(updateJob);
            }

            return updatePacket;
        }

        internal void DoUpdate(UpdatePacket update)
        {
            foreach (var job in update.Jobs)
            {
                _state.SetUpdatingTo(job.LocalMod, job.Target.Hash, job.Target.Display);
                SyncManager.QueueJob(job);
            }
        }

        public void PrintInternalState() => _state.PrintState();

        public UpdateTarget GetUpdateTarget(StorageMod mod) => _state.GetUpdateTarget(mod.Mod);

        public List<JobView> GetAllJobs() => SyncManager.GetAllJobs().Select(j => new JobView(j)).ToList();
        public List<JobView> GetActiveJobs() => SyncManager.GetActiveJobs().Select(j => new JobView(j)).ToList();
    }
}