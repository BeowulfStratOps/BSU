using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.JobManager;
using BSU.Core.Model.Updating;
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

        private readonly JobSlot _loadJob;
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
            IUpdateCreate updateState = null)
        {
            _internalState = internalState;
            _jobManager = jobManager;
            _parentIdentifier = parentIdentifier;
            CanWrite = canWrite;
            ActionQueue = actionQueue;
            Implementation = implementation;
            Identifier = identifier;
            _loadJob = new JobSlot(LoadInternal, $"Loading storageMod {identifier}", jobManager);
            _matchHashJob = new JobSlot(CreateMatchHash,
                $"Creating MatchHash for storageMod {identifier}", jobManager);
            _versionHashJob = new JobSlot(CreateVersionHash,
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
                _matchHash = updateState.GetTargetMatch();
                _versionHash = updateState.GetTargetVersion();
            }
        }

        public void Load()
        {
            _loadJob.Request();
        }

        private void LoadInternal(CancellationToken cancellationToken)
        {
            // TODO: remove all this code duplication.
            // TODO: use cancellationToken
            try
            {
                Implementation.Load();
                ActionQueue.EnQueueAction(() =>
                {
                    State = StorageModStateEnum.Loaded;
                });
            }

            catch (Exception e)
            {
                _error = e;
                ActionQueue.EnQueueAction(() => State = StorageModStateEnum.Error);
            }
        }

        private void CreateVersionHash(CancellationToken cancellationToken)
        {
            if (_state == StorageModStateEnum.Created) throw new InvalidOperationException();
            // TODO: use cancellationToken
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

        private void CreateMatchHash(CancellationToken cancellationToken)
        {
            if (_state == StorageModStateEnum.Created) throw new InvalidOperationException();
            // TODO: use cancellationToken
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

        public VersionHash GetVersionHash()
        {
            if (_versionHash == null) throw new InvalidOperationException();
            return _versionHash;
        }

        public MatchHash GetMatchHash()
        {
            if (_matchHash == null) throw new InvalidOperationException();
            return _matchHash;
        }

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

        private void SetCurrentUpdate(IUpdateCreate update)
        {
            // Don't actually need to save it. just handle when it's done
            update.OnEnded += () =>
            {
                _versionHashJob.Reset();
                _matchHashJob.Reset();
                UpdateTarget = null;
                State = StorageModStateEnum.Loaded;
            };
        }

        public IUpdateCreate PrepareUpdate(IRepositoryMod repositoryMod, UpdateTarget target, MatchHash targetMatch, VersionHash targetVersion)
        {
            CheckState(StorageModStateEnum.Versioned, StorageModStateEnum.CreatedWithUpdateTarget);

            if (_loadJob.IsRunning || _matchHashJob.IsRunning || _versionHashJob.IsRunning) throw new InvalidOperationException();

            // TODO: bit of redundancy between those hashes and update target?
            _matchHash = targetMatch;
            _versionHash = targetVersion;

            var update = new StorageModUpdateState(_jobManager, repositoryMod, this, target, targetMatch, targetVersion);

            UpdateTarget = target;
            State = StorageModStateEnum.Updating;

            SetCurrentUpdate(update);

            return update;
        }

        public void Abort()
        {
            CheckState(StorageModStateEnum.CreatedWithUpdateTarget);
            UpdateTarget = null;
            // TODO: should we call Implementation.Load somehow?
            State = StorageModStateEnum.Loaded;
        }

        public PersistedSelection GetStorageModIdentifiers()
        {
            return new PersistedSelection(_parentIdentifier, Identifier);
        }

        public override string ToString() => Identifier;

        public object GetUid() => _logger.GetId();

        public void RequireMatchHash()
        {
            CheckState(StorageModStateEnum.Loaded);
            _matchHashJob.Request();
        }

        public void RequireVersionHash()
        {
            CheckState(StorageModStateEnum.Matched);
            _versionHashJob.Request();
        }
    }
}
