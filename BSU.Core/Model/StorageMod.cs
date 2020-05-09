using System;
using System.Linq;
using BSU.Core.Hashes;
using BSU.Core.JobManager;
using BSU.Core.Sync;
using BSU.Core.View; // TODO: wtf
using BSU.CoreCommon;
using NLog;

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

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private StorageModStateEnum _state;
        
        private StorageModStateEnum State
        {
            get => _state;
            set
            {
                var old = _state;
                _state = value;
                _logger.Trace("State of '{0}' changed from {1} to {2}.", Identifier, old, value);
                StateChanged?.Invoke();
            }
        }
        
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
                State = StorageModStateEnum.Loaded;
            };
            _hashing.OnFinished += () =>
            {
                State = StorageModStateEnum.Hashed;
            };
            _updating = new ManualJobSlot<RepoSync>(jobManager);
            _updating.OnFinished += () =>
            {
                lock (_stateLock)
                {
                    _versionHash = null;
                    _matchHash = null;
                    UpdateTarget = null;
                    _loading.StartJob();
                    State = StorageModStateEnum.Loading;
                }
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
            if (!expected.Contains(State)) throw new InvalidOperationException(State.ToString());
        }

        public void RequireHash()
        {
            lock (_stateLock)
            {
                CheckState(StorageModStateEnum.Loaded);
                _hashing.StartJob();
                State = StorageModStateEnum.Hashing;
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
            // TODO: building this may take some time. should be called async
            lock (_stateLock)
            {
                // TODO: state lock? for this? for repo mod?
                CheckState(StorageModStateEnum.CreatedForDownload, StorageModStateEnum.Hashed, StorageModStateEnum.CreatedWithUpdateTarget);
                var title =
                    $"Updating {Storage.Location}/{Identifier} to {repositoryMod.Implementation.GetDisplayName()}";
                var target = new UpdateTarget(repositoryMod.GetState().VersionHash.GetHashString(),
                    repositoryMod.Implementation.GetDisplayName());
                UpdateTarget = target;
                var repoSync = new RepoSync(repositoryMod, this, target, title, 0);
                _versionHash = null;
                _matchHash = null;
                _updating.StartJob(repoSync);
                State = StorageModStateEnum.Updating;
                return repoSync;
            }
        }

        public StorageModState GetState()
        {
            lock(_stateLock)
            {
                var job = _updating.GetJob();
                var jobTarget = job?.Target;
                return new StorageModState(_matchHash, _versionHash, UpdateTarget, jobTarget, State);
            }
        }
    }
}