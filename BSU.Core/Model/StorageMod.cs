using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using BSU.Core.Hashes;
using BSU.Core.JobManager;
using BSU.Core.Sync;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class StorageMod
    {
        // TODO: come up with a proper state machine implementation
        
        private readonly IInternalState _internalState;
        private readonly IJobManager _jobManager;
        public Storage Storage { get; }
        public string Identifier { get; }
        public IStorageMod Implementation { get; }
        public Uid Uid { get; } = new Uid();

        private readonly JobSlot<SimpleJob> _loading;
        private readonly JobSlot<SimpleJob> _hashing;

        private MatchHash _matchHash;
        private VersionHash _versionHash;

        private readonly RepoSyncSlot _updating;
        private UpdateTarget _updateTarget;

        private readonly object _stateLock = new object(); // TODO: use it!!!

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private StorageModStateEnum _state; // TODO: should not be directly accessible
        
        public readonly List<ModAction> RelatedModActions = new List<ModAction>();

        private Exception _error;

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

        public StorageMod(Storage parent, IStorageMod implementation, string identifier, UpdateTarget updateTarget,
            IInternalState internalState, IJobManager jobManager)
        {
            _internalState = internalState;
            _jobManager = jobManager;
            Storage = parent;
            Implementation = implementation;
            Identifier = identifier;
            var title1 = $"Load StorageMod {Identifier}";
            _loading = new JobSlot<SimpleJob>(() => new SimpleJob(LoadJob, title1, 1), title1, jobManager);
            var title2 = $"Hash StorageMod {Identifier}";
            _hashing = new JobSlot<SimpleJob>(() => new SimpleJob(HashJob, title2, 1), title2, jobManager);
            /*_loading.OnFinished += error =>
            {
                _error = error; // TODO 
                State = error == null ? StorageModStateEnum.Loaded : StorageModStateEnum.ErrorLoad;
            };*/
            /*_hashing.OnFinished += error =>
            {
                _error = error;
                State = error == null ? StorageModStateEnum.Hashed : StorageModStateEnum.ErrorLoad;
            }; // TODO: check error*/
            _updating = new RepoSyncSlot(jobManager);
            _updating.OnFinished += error =>
            {
                lock (_stateLock)
                {
                    _versionHash = null;
                    _matchHash = null;
                    
                    if (error == null)
                    {
                        UpdateTarget = null;
                        _loading.StartJob();
                        State = StorageModStateEnum.Loading;
                        return;
                    }
                    
                    _error = error;
                    State = StorageModStateEnum.ErrorUpdate;
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

        private void LoadJob(CancellationToken cancellationToken)
        {
            // TODO: use cancellationToken
            Implementation.Load();
            var matchHash = new MatchHash(Implementation);
            Storage.Model.EnQueueAction(() =>
            {
                _matchHash = matchHash;
                State = StorageModStateEnum.Loaded;
            });
        }

        private void HashJob(CancellationToken cancellationToken)
        {
            // TODO: use cancellationToken
            var versionHash = new VersionHash(Implementation);
            Storage.Model.EnQueueAction(() =>
            {
                _versionHash = versionHash;
                State = StorageModStateEnum.Hashed;
            });
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

        internal IUpdateState PrepareUpdate(RepositoryMod repositoryMod, Action rollback = null)
        {
            lock (_stateLock)
            {
                // TODO: state lock for repo mod?
                CheckState(StorageModStateEnum.CreatedForDownload, StorageModStateEnum.Hashed,
                    StorageModStateEnum.CreatedWithUpdateTarget);
                var target = new UpdateTarget(repositoryMod.GetState().VersionHash.GetHashString(),
                    repositoryMod.Implementation.GetDisplayName());
                UpdateTarget = target;
                var title =
                    $"Updating {Storage.Location}/{Identifier} to {repositoryMod.Implementation.GetDisplayName()}";

                _updating.Prepare(repositoryMod, this, target, title, rollback);
                
                _versionHash = null;
                _matchHash = null;

                State = StorageModStateEnum.Updating;

                return _updating; // TODO: meh. should be a new object every time.
            }
        }

        public StorageModState GetState()
        {
            lock (_stateLock)
            {
                return new StorageModState(_matchHash, _versionHash, UpdateTarget, _updating.Target, State, _error);
            }
        }

        public void Abort()
        {
            lock (_stateLock)
            {
                CheckState(StorageModStateEnum.CreatedWithUpdateTarget);
                UpdateTarget = null;
                _loading.StartJob();
                State = StorageModStateEnum.Loading;
            }
        }
    }
}