using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BSU.Core.Hashes;
using BSU.Core.JobManager;
using BSU.Core.Persistence;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class StorageMod : IModelStorageMod
    {
        // TODO: come up with a proper state machine implementation
        
        private readonly IStorageModState _internalState;
        private readonly IJobManager _jobManager;
        private readonly Guid _parentIdentifier;
        public bool CanWrite { get; }
        public string Identifier { get; }
        private IActionQueue ActionQueue { get; }
        public IStorageMod Implementation { get; }
        public Uid Uid { get; } = new Uid();

        private readonly JobSlot<SimpleJob> _loading;
        private readonly JobSlot<SimpleJob> _hashing;

        private MatchHash _matchHash;
        private VersionHash _versionHash;

        private StorageModUpdateState _updating;
        private UpdateTarget _updateTarget;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private StorageModStateEnum _state; // TODO: should not be directly accessible

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

        public StorageMod(IActionQueue actionQueue, IStorageMod implementation, string identifier, UpdateTarget updateTarget,
            IStorageModState internalState, IJobManager jobManager, Guid parentIdentifier, bool canWrite, IUpdateState downloadState = null)
        {
            _internalState = internalState;
            _jobManager = jobManager;
            _parentIdentifier = parentIdentifier;
            CanWrite = canWrite;
            ActionQueue = actionQueue;
            Implementation = implementation;
            Identifier = identifier;
            var title1 = $"Load StorageMod {Identifier}";
            _loading = new JobSlot<SimpleJob>(() => new SimpleJob(LoadJob, title1, 1), title1, jobManager);
            var title2 = $"Hash StorageMod {Identifier}";
            _hashing = new JobSlot<SimpleJob>(() => new SimpleJob(HashJob, title2, 1), title2, jobManager);
            _loading.OnFinished += error =>
            {
                if (error == null) return;
                _error = error;
                State = StorageModStateEnum.ErrorLoad;
            };
            _hashing.OnFinished += error =>
            {
                if (error == null) return;
                _error = error;
                State = StorageModStateEnum.ErrorLoad;
            };
            if (updateTarget == null)
            {
                _updateTarget = _internalState.UpdateTarget;
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
                if (downloadState == null) throw new ArgumentNullException(nameof(downloadState));
                _versionHash = VersionHash.CreateEmpty();
                UpdateTarget = updateTarget;
                downloadState.OnEnded += () => UpdateStateEnd(downloadState);
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
            CheckState(StorageModStateEnum.Loaded);
            _hashing.StartJob();
            State = StorageModStateEnum.Hashing;
        }

        private void LoadJob(CancellationToken cancellationToken)
        {
            // TODO: use cancellationToken
            Implementation.Load();
            var matchHash = new MatchHash(Implementation);
            ActionQueue.EnQueueAction(() =>
            {
                _matchHash = matchHash;
                State = StorageModStateEnum.Loaded;
            });
        }

        private void HashJob(CancellationToken cancellationToken)
        {
            // TODO: use cancellationToken
            var versionHash = new VersionHash(Implementation);
            ActionQueue.EnQueueAction(() =>
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
                _internalState.UpdateTarget = value;
            }
        }

        public event Action StateChanged;

        public IUpdateState PrepareUpdate(IRepositoryMod repositoryMod, UpdateTarget target)
        {
            
            CheckState(StorageModStateEnum.CreatedForDownload, StorageModStateEnum.Hashed, StorageModStateEnum.CreatedWithUpdateTarget);
            if (_updating != null) throw new InvalidOperationException();
            
            var update = new StorageModUpdateState(_jobManager, ActionQueue, repositoryMod, this, target);
                
            UpdateTarget = target;
            _updating = update;
            _versionHash = null;
            State = StorageModStateEnum.Updating;

            update.OnEnded += () => UpdateStateEnd(update);

            return update;
        }

        private void UpdateStateEnd(IUpdateState update)
        {
            _versionHash = null;
            _matchHash = null;
            _updating = null;

            if (update.Exception == null)
            {
                UpdateTarget = null;
                _loading.StartJob();
                State = StorageModStateEnum.Loading;
                return;
            }

            _error = update.Exception;
            State = StorageModStateEnum.ErrorUpdate;
        }

        public StorageModState GetState()
        {
            return new StorageModState(_matchHash, _versionHash, UpdateTarget, _updating?.Target, State, _error);
        }

        public void Abort()
        {
            CheckState(StorageModStateEnum.CreatedWithUpdateTarget);
            UpdateTarget = null;
            _loading.StartJob();
            State = StorageModStateEnum.Loading;
        }

        public PersistedSelection GetStorageModIdentifiers()
        {
            return new PersistedSelection(_parentIdentifier, Identifier);
        }

        public override string ToString() => Identifier;
    }
}