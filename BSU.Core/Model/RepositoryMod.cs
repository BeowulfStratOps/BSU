using System;
using System.Collections.Generic;
using System.Diagnostics;
using BSU.Core.Hashes;
using BSU.Core.Model.Actions;
using BSU.Core.Services;
using BSU.Core.View;
using BSU.CoreCommon;
using StorageTarget = BSU.Core.Model.Actions.StorageTarget;

namespace BSU.Core.Model
{
    internal class RepositoryMod
    {
        public Repository Repository { get; }
        public IRepositoryMod Implementation { get; }
        public string Identifier { get; }
        public Uid Uid { get; } = new Uid();
        
        public MatchHash MatchHash { private set; get; }
        public VersionHash VersionHash { private set; get; }

        public Dictionary<StorageTarget, ModAction> Actions { get; } = new Dictionary<StorageTarget, ModAction>();
        
        public JobSlot<SimpleJob> Loading { get; }

        private UpdateTarget _updateTarget;

        public RepositoryMod(Repository parent, IRepositoryMod implementation, string identifier)
        {
            Repository = parent;
            Implementation = implementation;
            Identifier = identifier;
            Loading = new JobSlot<SimpleJob>(() => new SimpleJob(Load, $"Load RepoMod {Identifier}", 1));
            Loading.StartJob();
        }

        private void AddStorage(Storage storage)
        {
            Actions[storage.AsTarget] = new DownloadAction(storage, this, _updateTarget);
            ActionAdded?.Invoke(storage.AsTarget);
        }

        private void Load()
        {
            Implementation.Load();
            MatchHash = new MatchHash(this);
            VersionHash = new VersionHash(this);
            _updateTarget = new UpdateTarget(VersionHash.GetHashString(), Implementation.GetDisplayName());
            Repository.Model.MatchMaker.AddRepoMod(this);
            
            // TODO: move this to match maker?
            Repository.Model.StorageAdded += AddStorage;
            foreach (var storage in Repository.Model.Storages)
            {
                AddStorage(storage);
            }
        }

        internal void AddMatch(StorageMod storageMod)
        {
            Actions.Add(storageMod.AsTarget, new LoadingAction(_updateTarget));
            ActionAdded?.Invoke(storageMod.AsTarget);
            storageMod.StateChanged += () => CheckModVersion(storageMod);
            if (storageMod.VersionHash == null)
            {
                storageMod.Hashing.StartJob();
            }
            else
                CheckModVersion(storageMod);
        }

        public event Action<StorageTarget> ActionAdded;
        public event Action<StorageTarget> ActionChanged;

        internal void CheckModVersion(StorageMod storageMod)
        {
            //Logger.Debug("Checking local match {0}", storageMod.Uid);
            ModAction action = null;

            if (!storageMod.Updating.IsActive())
            {
                if (storageMod.VersionHash == null)
                {
                    storageMod.Hashing.StartJob();
                    action = new LoadingAction(_updateTarget);
                }
                else
                {
                    if (VersionHash.IsMatch(storageMod.VersionHash) && storageMod.UpdateTarget == null)
                        action = new UseAction(storageMod, _updateTarget);
                    else
                    {
                        if (storageMod.Storage.Implementation.CanWrite())
                        {
                            // TODO: can this be checked earlier?
                            var continuation = ServiceProvider.InternalState.GetUpdateTarget(storageMod) != null;
                            action = new UpdateAction(storageMod, this, continuation, _updateTarget);
                        }
                    }
                }
            }
            else
            {
                if (storageMod.Updating.GetJob().Target.Hash == VersionHash.GetHashString())
                    action = new AwaitUpdateAction(storageMod, _updateTarget);
                else
                    throw new InvalidOperationException("Wtf...");
            }

            //Logger.Debug("Created action: {0}", action);


            Actions[storageMod.AsTarget] = action;
            ActionChanged?.Invoke(storageMod.AsTarget);
            
            //storageMod.AddRelatedAction(action);
        
            // TODO: pre-select an action
            // TODO: build conflicts
        }
    }
}