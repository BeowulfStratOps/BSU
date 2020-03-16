using System;
using BSU.Core.Hashes;
using BSU.Core.Services;
using BSU.Core.Sync;
using BSU.Core.View; // TODO: wtf
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal class StorageMod
    {
        public Storage Storage { get; }
        public string Identifier { get; }
        public IStorageMod Implementation { get; }
        public Uid Uid { get; } = new Uid();

        private readonly JobSlot<SimpleJob> _loading;
        private readonly JobSlot<SimpleJob> _hashing;

        private MatchHash _matchHash;
        private VersionHash _versionHash;

        private readonly ManualJobSlot<RepoSync> _updating;
        private UpdateTarget _updateTarget;

        private bool _requireHashing;
        
        private readonly object _stateLock = new object(); // TODO: use it!!!
        
        public StorageMod(Storage parent, IStorageMod implementation, string identifier, UpdateTarget updateTarget)
        {
            Storage = parent;
            Implementation = implementation;
            Identifier = identifier;
            _loading = new JobSlot<SimpleJob>(() => new SimpleJob(Load, $"Load StorageMod {Identifier}", 1));
            _hashing = new JobSlot<SimpleJob>(() => new SimpleJob(Hash, $"Hash StorageMod {Identifier}", 1));
            _loading.OnFinished += () =>
            {
                StateChanged?.Invoke();
                if (_requireHashing) _hashing.StartJob();
            };
            _hashing.OnFinished += () => StateChanged?.Invoke();
            _updating = new ManualJobSlot<RepoSync>();
            _updating.OnStarted += () => StateChanged?.Invoke();
            _updating.OnFinished += () =>
            {
                _versionHash = null;
                _matchHash = null;
                _requireHashing = false;
                _loading.StartJob();
                StateChanged?.Invoke();
            };
            UpdateTarget = updateTarget ?? ServiceProvider.InternalState.GetUpdateTarget(this);
            _loading.StartJob();
        }

        public void RequireHash()
        {
            lock (_stateLock)
            {
                if (_requireHashing) return;
                _requireHashing = true;
                if (!_updating.IsActive()) _hashing.StartJob();
            }
        }

        private void Load()
        {
            Implementation.Load();
            _matchHash = new MatchHash(Implementation);
        }

        private void Hash()
        {
            _versionHash = new VersionHash(Implementation);
            if (_versionHash.GetHashString() == UpdateTarget?.Hash)
                UpdateTarget = null;
        }

        private UpdateTarget UpdateTarget
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
        
        internal RepoSync StartUpdate(RepositoryMod repositoryMod)
        {
            // TODO: state lock? for this? for repo mod?
            var title = $"Updating {Storage.Location}/{Identifier} to {repositoryMod.Implementation.GetDisplayName()}";
            var target = new UpdateTarget(repositoryMod.GetState().VersionHash.GetHashString(), repositoryMod.Implementation.GetDisplayName());
            var repoSync = new RepoSync(repositoryMod, this, target, title, 0);
            _updating.StartJob(repoSync);
            return repoSync;
        }

        public StorageModState GetState()
        {
            lock(_stateLock)
            {
                var job = _updating.GetJob();
                var jobTarget = job?.Target;
                return new StorageModState(_matchHash, _versionHash, UpdateTarget, jobTarget, _requireHashing);
            }
        }
    }
}