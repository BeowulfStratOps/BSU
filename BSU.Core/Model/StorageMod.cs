using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        private readonly IPersistedStorageModState _internalState;
        private readonly IJobManager _jobManager;
        private readonly Guid _parentIdentifier;
        public bool CanWrite { get; }
        public string Identifier { get; }
        private IActionQueue ActionQueue { get; }
        public IStorageMod Implementation { get; }

        private readonly AsyncJobSlot _loading;
        private readonly JobSlot _matchHashJob;
        private readonly JobSlot _versionHashJob;

        private UpdateTarget _updateTarget;

        private MatchHash _matchHash;
        private VersionHash _versionHash;

        private readonly Logger _logger = EntityLogger.GetLogger();

        private StorageModStateEnum _state = StorageModStateEnum.Created; // TODO: should not be directly accessible

        private Exception _error;

        private StorageModStateEnum State
        {
            get => _state;
            set
            {
                var old = _state;
                _state = value;
                _logger.Debug("State of '{0}' changed from {1} to {2}.", Identifier, old, value);
                StateChanged?.Invoke();
            }
        }

        public StorageMod(IActionQueue actionQueue, IStorageMod implementation, string identifier,
            IPersistedStorageModState internalState, IJobManager jobManager, Guid parentIdentifier, bool canWrite,
            IUpdateState updateState = null)
        {
            _internalState = internalState;
            _jobManager = jobManager;
            _parentIdentifier = parentIdentifier;
            CanWrite = canWrite;
            ActionQueue = actionQueue;
            Implementation = implementation;
            Identifier = identifier;
            _loading = new AsyncJobSlot(_ => Implementation.Load(), $"Loading storageMod {identifier}", jobManager);
            _matchHashJob = new JobSlot(_ => CreateMatchHash(),
                $"Creating MatchHash for storageMod {identifier}", jobManager);
            _versionHashJob = new JobSlot(_ => CreateVersionHash(),
                $"Creating VersionHash for storageMod {identifier}", jobManager);

            _updateTarget = _internalState.UpdateTarget;
            if (_updateTarget != null)
            {
                _state = StorageModStateEnum.CreatedWithUpdateTarget;
                _versionHash = VersionHash.FromDigest(_updateTarget.Hash);
            }

            if (updateState != null)
            {
                _state = StorageModStateEnum.Updating;
                SetCurrentUpdate(updateState);
            }
        }

        private void CreateVersionHash()
        {
            try
            {
                _versionHash = new VersionHash(Implementation);

                ActionQueue.EnQueueAction(() =>
                {
                    State = StorageModStateEnum.Versioned;
                });
            }
            catch (Exception e)
            {
                _error = e;

                ActionQueue.EnQueueAction(() =>
                {
                    State = StorageModStateEnum.Error;
                });
            }
        }

        private void CreateMatchHash()
        {
            try
            {
                _matchHash = new MatchHash(Implementation);

                ActionQueue.EnQueueAction(() =>
                {
                    State = StorageModStateEnum.Matched;
                });
            }
            catch (Exception e)
            {
                _error = e;

                ActionQueue.EnQueueAction(() =>
                {
                    State = StorageModStateEnum.Error;
                });
            }
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void CheckState(params StorageModStateEnum[] expected)
        {
            if (!expected.Contains(State)) throw new InvalidOperationException(State.ToString());
        }

        public VersionHash GetVersionHash() => _versionHash;

        public MatchHash GetMatchHash() => _matchHash;

        public StorageModStateEnum GetState() => State;

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

        private void SetCurrentUpdate(IUpdateState update)
        {
            // Don't actually need to save it. just handle when it's done
            update.OnEnded += () =>
            {
                _versionHashJob.Reset();
                _matchHashJob.Reset();
                UpdateTarget = null;
                State = StorageModStateEnum.Created;
            };
        }

        public IUpdateState PrepareUpdate(IRepositoryMod repositoryMod, UpdateTarget target, MatchHash targetMatch, VersionHash targetVersion)
        {
            CheckState(StorageModStateEnum.Created, StorageModStateEnum.CreatedWithUpdateTarget);

            if (_loading.IsRunning || _matchHashJob.IsRunning || _versionHashJob.IsRunning) throw new InvalidOperationException();

            // TODO: bit of redundancy between those hashes and update target?
            _matchHash = targetMatch;
            _versionHash = targetVersion;

            var update = new StorageModUpdateState(_jobManager, ActionQueue, repositoryMod, this, target);

            UpdateTarget = target;
            State = StorageModStateEnum.Updating;

            SetCurrentUpdate(update);

            return update;
        }

        public void Abort()
        {
            CheckState(StorageModStateEnum.CreatedWithUpdateTarget);
            UpdateTarget = null;
            State = StorageModStateEnum.Created;
        }

        public PersistedSelection GetStorageModIdentifiers()
        {
            return new PersistedSelection(_parentIdentifier, Identifier);
        }

        public override string ToString() => Identifier;

        public object GetUid() => _logger.GetId();

        public void RequireMatchHash()
        {
            _matchHashJob.Request();
        }

        public void RequireVersionHash()
        {
            _versionHashJob.Request();
        }
    }
}
