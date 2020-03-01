using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.Core.Model.Actions;
using BSU.Core.Services;
using BSU.Core.Sync;
using DownloadAction = BSU.Core.Model.Actions.DownloadAction;
using UpdateAction = BSU.Core.Model.Actions.UpdateAction;

namespace BSU.Core.Model
{
    internal class Model
    {
        // TODO: lock enumerables while stuff is being executed!!!
        internal MatchMaker MatchMaker { get; } = new MatchMaker();
        public List<Repository> Repositories { get; } = new List<Repository>();
        public List<Storage> Storages { get; } = new List<Storage>();

        private InternalState PersistentState { get; }

        public Model(InternalState persistentState)
        {
            PersistentState = persistentState;
        }

        public void Load()
        {
            foreach (var repository in PersistentState.LoadRepositories())
            {
                repository.Model = this;
                Repositories.Add(repository);
                RepositoryAdded?.Invoke(repository);
            }
            foreach (var storage in PersistentState.LoadStorages())
            {
                storage.Model = this;
                Storages.Add(storage);
                StorageAdded?.Invoke(storage);
            }
        }
        
        internal UpdatePacket PrepareUpdate(List<ModAction> actions)
        {
            //Logger.Debug("Preparing update");

            var updatePacket = new UpdatePacket();

            foreach (var downloadAction in actions.OfType<DownloadAction>())
            {
                updatePacket.Rollback.Add(() =>
                    downloadAction.Storage.Implementation.RemoveMod(downloadAction.FolderName));
                var storageMod = downloadAction.Storage.CreateMod(downloadAction.FolderName, downloadAction.UpdateTarget);
                var syncState = new RepoSync(downloadAction.RepositoryMod, storageMod, downloadAction.UpdateTarget, downloadAction.ToString(), 0);
                updatePacket.Jobs.Add(syncState);
            }


            foreach (var updateAction in actions.OfType<UpdateAction>())
            {
                var syncState = new RepoSync(updateAction.RepositoryMod, updateAction.StorageMod,
                    updateAction.UpdateTarget, updateAction.ToString(), 0);
                updatePacket.Jobs.Add(syncState);
            }

            return updatePacket;
        }

        internal void DoUpdate(UpdatePacket update)
        {
            //Logger.Debug("Doing update");
            foreach (var job in update.Jobs)
            {
                if (!(job is RepoSync sync)) throw new InvalidCastException("WTF..");
                ServiceProvider.InternalState.SetUpdatingTo(sync.StorageMod, sync.Target.Hash, sync.Target.Display);
                sync.StorageMod.Updating.StartJob(sync);
            }
        }

        public event Action<Repository> RepositoryAdded;
        public event Action<Storage> StorageAdded;

        public void AddRepository(string type, string url, string name)
        {
            var repository = PersistentState.AddRepo(name, url, type, this);
            Repositories.Add(repository);
            RepositoryAdded?.Invoke(repository);
        }
        
        public void AddStorage(string type, DirectoryInfo dir, string name)
        {
            var storage = PersistentState.AddStorage(name, dir, type);
            Storages.Add(storage);
            StorageAdded?.Invoke(storage);
        }
    }
}