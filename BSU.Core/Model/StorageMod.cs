using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Hashes;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.Core.Sync;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class StorageMod : IModelStorageMod
    {
        private readonly IPersistedStorageModState _internalState;
        public bool CanWrite { get; }
        public string Identifier { get; }
        public IModelStorage ParentStorage { get; }
        public bool IsDeleted { get; private set; }
        private readonly IStorageMod _implementation;

        private MatchHash _matchHash;
        private VersionHash _versionHash;
        private string _title;

        private UpdateTarget _updateTarget;

        private readonly ILogger _logger;

        private StorageModStateEnum _state = StorageModStateEnum.Loading; // TODO: should not be directly accessible
        private readonly IEventBus _eventBus;

        private StorageModStateEnum State
        {
            get => _state;
            set
            {
                var old = _state;
                _state = value;
                _logger.Debug($"State changed from {old} to {value}");
                StateChanged?.Invoke(this);
            }
        }

        public StorageMod(IStorageMod implementation, string identifier,
            IPersistedStorageModState internalState, IModelStorage parent, bool canWrite, IEventBus eventBus)
        {
            _logger = LogHelper.GetLoggerWithIdentifier(this, identifier);
            _internalState = internalState;
            ParentStorage = parent;
            CanWrite = canWrite;
            _eventBus = eventBus;
            _implementation = implementation;
            Identifier = identifier;

            _updateTarget = _internalState.UpdateTarget;
            if (_updateTarget != null)
            {
                _state = StorageModStateEnum.CreatedWithUpdateTarget;
                _versionHash = VersionHash.FromDigest(_updateTarget.Hash);
                _title = identifier;
            }
            else
            {
                Load();
            }
        }

        private async Task<(MatchHash matchHash, VersionHash versionHash, string title)> LoadAsync(CancellationToken cancellationToken)
        {
            var versionHash = await VersionHash.CreateAsync(_implementation, cancellationToken);
            var matchHash = await MatchHash.CreateAsync(_implementation, cancellationToken);
            var title = await _implementation.GetTitle(cancellationToken);

            return (matchHash, versionHash, title);
        }

        private void Load()
        {
            Task.Run(() => LoadAsync(CancellationToken.None)).ContinueInEventBus(_eventBus, getResult =>
            {
                try
                {
                    (_matchHash, _versionHash, _title) = getResult();
                    SetState(StorageModStateEnum.Created, StorageModStateEnum.Loading);
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                    // TOOD: should this be reported to user directly?
                    SetState(StorageModStateEnum.Error, StorageModStateEnum.Loading);
                }
            });
        }

        public VersionHash GetVersionHash()
        {
            if (State == StorageModStateEnum.Loading) throw new InvalidOperationException($"Not allowed in State {State}");
            return _versionHash;
        }

        public MatchHash GetMatchHash()
        {
            if (State == StorageModStateEnum.Loading) throw new InvalidOperationException($"Not allowed in State {State}");
            return _matchHash;
        }

        public StorageModStateEnum GetState() => State;

        public string GetTitle() => _title;

        public void Delete(bool removeData)
        {
            if (removeData) throw new NotImplementedException();
            IsDeleted = true;
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

        public event Action<IModelStorageMod> StateChanged;

        public IModUpdate PrepareUpdate(IRepositoryMod repositoryMod, MatchHash targetMatch, VersionHash targetVersion, IProgress<FileSyncStats> progress)
        {
            _logger.Trace("Progress: Waiting");
            progress?.Report(new FileSyncStats(FileSyncState.Waiting));
            _matchHash = targetMatch;
            _versionHash = targetVersion;
            UpdateTarget = new UpdateTarget(targetVersion.GetHashString());
            SetState(StorageModStateEnum.Updating, StorageModStateEnum.Created, StorageModStateEnum.CreatedWithUpdateTarget);

            var update = new StorageModUpdateState(this, _implementation, repositoryMod, progress);

            update.OnEnded += () =>
            {
                _eventBus.ExecuteSynchronized(() =>
                {

                    UpdateTarget = null;
                    SetState(StorageModStateEnum.Created, StorageModStateEnum.Updating);
                });
            };

            return update;
        }

        private void SetState(StorageModStateEnum newState, params StorageModStateEnum[] acceptableCurrent)
        {
            if (!acceptableCurrent.Contains(State)) throw new InvalidOperationException($"Tried to transition from {State} to {newState}");
            State = newState;
        }

        public void Abort()
        {
            SetState(StorageModStateEnum.Created, StorageModStateEnum.CreatedWithUpdateTarget);
        }

        public PersistedSelection GetStorageModIdentifiers()
        {
            return new PersistedSelection(PersistedSelectionType.StorageMod, ParentStorage.Identifier, Identifier);
        }

        public override string ToString() => Identifier;
    }
}
