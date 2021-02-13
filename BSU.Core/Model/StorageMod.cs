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
        public Uid Uid { get; } = new Uid();

        private readonly JobSlot _loading;
        private readonly FuncSlot<MatchHash> _matchHashJob;
        private readonly FuncSlot<VersionHash> _versionHashJob;

        private UpdateTarget _updateTarget;

        private MatchHash _updateMatchHash;
        private VersionHash _updateVersionHash;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private StorageModStateEnum _state = StorageModStateEnum.Created; // TODO: should not be directly accessible

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

        public StorageMod(IActionQueue actionQueue, IStorageMod implementation, string identifier,
            IPersistedStorageModState internalState, IJobManager jobManager, Guid parentIdentifier, bool canWrite)
        {
            _internalState = internalState;
            _jobManager = jobManager;
            _parentIdentifier = parentIdentifier;
            CanWrite = canWrite;
            ActionQueue = actionQueue;
            Implementation = implementation;
            Identifier = identifier;
            _loading = new JobSlot(_ => Implementation.Load(), $"Loading storageMod {identifier}", jobManager);
            _matchHashJob = new FuncSlot<MatchHash>(_ => new MatchHash(implementation),
                $"Creating MatchHash for storageMod {identifier}", jobManager);
            _versionHashJob = new FuncSlot<VersionHash>(_ => new VersionHash(implementation),
                $"Creating MatchHash for storageMod {identifier}", jobManager);

            _updateTarget = _internalState.UpdateTarget;
            if (_updateTarget != null)
            {
                _state = StorageModStateEnum.CreatedWithUpdateTarget;
                _updateVersionHash = VersionHash.FromDigest(_updateTarget.Hash);
            }
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void CheckState(params StorageModStateEnum[] expected)
        {
            if (!expected.Contains(State)) throw new InvalidOperationException(State.ToString());
        }

        public async Task<VersionHash> GetVersionHash()
        {
            if (_state == StorageModStateEnum.Created)
            {
                await _loading.Do();
                return await _versionHashJob.Do();
            }
            if (_state == StorageModStateEnum.Updating)
            {
                return _updateVersionHash;
            }
            if (_state == StorageModStateEnum.CreatedWithUpdateTarget)
            {
                return _updateVersionHash;
            }
            throw new NotImplementedException();
        }

        public async Task<MatchHash> GetMatchHash()
        {
            if (_state == StorageModStateEnum.Created)
            {
                await _loading.Do();
                return await _matchHashJob.Do();
            }
            if (_state == StorageModStateEnum.Updating)
            {
                return _updateMatchHash;
            }
            throw new NotImplementedException();
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

        public IUpdateState PrepareUpdate(IRepositoryMod repositoryMod, UpdateTarget target, MatchHash targetMatch, VersionHash targetVersion)
        {
            CheckState(StorageModStateEnum.Created, StorageModStateEnum.CreatedWithUpdateTarget);

            if (_loading.IsRunning || _matchHashJob.IsRunning || _versionHashJob.IsRunning) throw new InvalidOperationException();

            // TODO: bit of redundancy between those hashes and update target?
            _updateMatchHash = targetMatch;
            _updateVersionHash = targetVersion;

            var update = new StorageModUpdateState(_jobManager, ActionQueue, repositoryMod, this, target);

            UpdateTarget = target;
            State = StorageModStateEnum.Updating;

            update.OnEnded += () =>
            {
                UpdateTarget = null;
                State = StorageModStateEnum.Created;
            };

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
    }
}
