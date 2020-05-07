using System;
using System.Linq;
using BSU.Core.Hashes;
using BSU.Core.JobManager;
using BSU.Core.Sync;
using BSU.Core.View; // TODO: wtf
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal class StorageMod
    {
        private readonly IInternalState _internalState;
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
        
        private readonly object _stateLock = new object(); // TODO: use it!!!

        private StorageModStateEnum _state;
        
        public StorageMod(Storage parent, IStorageMod implementation, string identifier, UpdateTarget updateTarget, IInternalState internalState, IJobManager jobManager)
        {
            _internalState = internalState;
            Storage = parent;
            Implementation = implementation;
            Identifier = identifier;
            var title1 = $"Load StorageMod {Identifier}";
            _loading = new JobSlot<SimpleJob>(() => new SimpleJob(LoadJob, title1, 1), title1, jobManager);
            var title2 = $"Hash StorageMod {Identifier}";
            _hashing = new JobSlot<SimpleJob>(() => new SimpleJob(HashJob, title2, 1), title2, jobManager);
            _loading.OnFinished += () =>
            {
                _state = StorageModStateEnum.Loaded;
                StateChanged?.Invoke();
            };
            _hashing.OnFinished += () =>
            {
                _state = StorageModStateEnum.Hashed;
                StateChanged?.Invoke();
            };
            _updating = new ManualJobSlot<RepoSync>(jobManager);
            _updating.OnStarted += () =>
            {
                _versionHash = null;
                _matchHash = null;
            };
            _updating.OnFinished += () =>
            {
                _versionHash = null;
                _matchHash = null;
                _loading.StartJob();
                _state = StorageModStateEnum.Loading;
                StateChanged?.Invoke();
            };
            if (updateTarget == null)
            {
                _updateTarget = _internalState.GetUpdateTarget(this);
                if (_updateTarget == null)
                {
                    _loading.StartJob();
                    _state = StorageModStateEnum.Loading;
                }
                else
                {
                    _state = StorageModStateEnum.CreatedWithUpdateTarget;
                }
            }
            else
            {
                _versionHash = VersionHash.CreateEmpty();
                UpdateTarget = updateTarget;
                _state = StorageModStateEnum.CreatedForDownload;
            }
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void CheckState(params StorageModStateEnum[] expected)
        {
            if (!expected.Contains(_state)) throw new InvalidOperationException(_state.ToString());
        }

        public void RequireHash()
        {
            lock (_stateLock)
            {
                CheckState(StorageModStateEnum.Loaded);
                _hashing.StartJob();
                _state = StorageModStateEnum.Hashing;
            }
        }

        private void LoadJob()
        {
            Implementation.Load();
            _matchHash = new MatchHash(Implementation);
        }

        private void HashJob()
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
                    _internalState.RemoveUpdatingTo(this);
                else
                    _internalState.SetUpdatingTo(this, value.Hash, value.Display);
            }
        }

        public event Action StateChanged;
        
        internal RepoSync StartUpdate(RepositoryMod repositoryMod)
        {
            lock (_stateLock)
            {
                // TODO: state lock? for this? for repo mod?
                CheckState(StorageModStateEnum.CreatedForDownload, StorageModStateEnum.Hashed, StorageModStateEnum.CreatedWithUpdateTarget);
                var title =
                    $"Updating {Storage.Location}/{Identifier} to {repositoryMod.Implementation.GetDisplayName()}";
                var target = new UpdateTarget(repositoryMod.GetState().VersionHash.GetHashString(),
                    repositoryMod.Implementation.GetDisplayName());
                var repoSync = new RepoSync(repositoryMod, this, target, title, 0);
                _updating.StartJob(repoSync);
                _state = StorageModStateEnum.Updating;
                return repoSync;
            }
        }

        public StorageModState GetState()
        {
            lock(_stateLock)
            {
                var job = _updating.GetJob();
                var jobTarget = job?.Target;
                return new StorageModState(_matchHash, _versionHash, UpdateTarget, jobTarget, _state);
            }
        }
    }
}