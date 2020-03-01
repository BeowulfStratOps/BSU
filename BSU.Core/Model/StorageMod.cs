using System;
using BSU.Core.Hashes;
using BSU.Core.Model.Actions;
using BSU.Core.Services;
using BSU.Core.Sync;
using BSU.Core.View;
using BSU.CoreCommon;
using StorageTarget = BSU.Core.Model.Actions.StorageTarget;

namespace BSU.Core.Model
{
    internal class StorageMod
    {
        public Storage Storage { get; }
        public string Identifier { get; }
        public IStorageMod Implementation { get; }
        public Uid Uid { get; } = new Uid();
        
        public JobSlot<SimpleJob> Loading { get; }
        public JobSlot<SimpleJob> Hashing { get; }
        
        public MatchHash MatchHash { private set; get; }
        public VersionHash VersionHash { private set; get; }
        
        public ManualJobSlot<RepoSync> Updating { get; }
        private UpdateTarget _updateTarget;
        
        public StorageMod(Storage parent, IStorageMod implementation, string identifier, bool newlyCreated = false)
        {
            Storage = parent;
            Implementation = implementation;
            Identifier = identifier;
            Loading = new JobSlot<SimpleJob>(() => new SimpleJob(Load, $"Load StorageMod {Identifier}", 1));
            Hashing = new JobSlot<SimpleJob>(() => new SimpleJob(Hash, $"Hash StorageMod {Identifier}", 1));
            Loading.OnFinished += () => StateChanged?.Invoke();
            Hashing.OnFinished += () => StateChanged?.Invoke();
            Updating = new ManualJobSlot<RepoSync>();
            Updating.OnStarted += () => StateChanged?.Invoke();
            Updating.OnFinished += () =>
            {
                VersionHash = null;
                Loading.StartJob();
                StateChanged?.Invoke();
            };
            _updateTarget = ServiceProvider.InternalState.GetUpdateTarget(this);
            if (!newlyCreated)
                Loading.StartJob();
            
        }

        private void Load()
        {
            Implementation.Load();
            MatchHash = new MatchHash(Implementation);
            Storage.Model.MatchMaker.AddStorageMod(this);
        }

        private void Hash()
        {
            VersionHash = new VersionHash(Implementation);
            if (VersionHash.GetHashString() == UpdateTarget?.Hash)
                UpdateTarget = null;
        }

        public UpdateTarget UpdateTarget
        {
            get => _updateTarget;
            set
            {
                _updateTarget = value;
                if (value == null)
                    ServiceProvider.InternalState.RemoveUpdatingTo(this);
                else
                    ServiceProvider.InternalState.SetUpdatingTo(this, value.Hash, value.Display);
            }
        }

        public event Action StateChanged;
        
        public StorageTarget AsTarget => new StorageTarget(this);
    }
}