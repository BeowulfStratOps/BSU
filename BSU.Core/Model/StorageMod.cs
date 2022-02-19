using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Hashes;
using BSU.Core.Ioc;
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

        private MatchHash? _matchHash;
        private VersionHash? _versionHash;
        private string? _title;

        private UpdateTarget? _updateTarget;

        private readonly ILogger _logger;

        private StorageModStateEnum _state = StorageModStateEnum.Loading; // TODO: should not be directly accessible
        private readonly IDispatcher _dispatcher;

        private StorageModStateEnum State
        {
            get => _state;
            set
            {
                var old = _state;
                _state = value;
                _logger.Debug($"Changing state from {old} to {value}");
                StateChanged?.Invoke(this);
            }
        }

        public StorageMod(IStorageMod implementation, string identifier,
            IPersistedStorageModState internalState, IModelStorage parent, bool canWrite, IServiceProvider services, MatchHash? createMatchHash = null)
        {
            _logger = LogHelper.GetLoggerWithIdentifier(this, identifier);
            _internalState = internalState;
            ParentStorage = parent;
            CanWrite = canWrite;
            _dispatcher = services.Get<IDispatcher>();
            _implementation = implementation;
            Identifier = identifier;

            _updateTarget = _internalState.UpdateTarget;
            if (_updateTarget != null)
            {
                _title = identifier;
                _versionHash = VersionHash.FromDigest(_updateTarget.Hash);
                if (createMatchHash != null)
                {
                    _state = StorageModStateEnum.CreatedWithUpdateTarget;
                    _matchHash = createMatchHash;
                }
                else
                {
                    Load(true);
                }
            }
            else
            {
                Load(false);
            }
        }

        private async Task<(MatchHash matchHash, VersionHash versionHash, string title)> LoadAsync(CancellationToken cancellationToken)
        {
            var versionHash = await VersionHash.CreateAsync(_implementation, cancellationToken);
            var matchHash = await MatchHash.CreateAsync(_implementation, cancellationToken);
            var title = await _implementation.GetTitle(cancellationToken);

            return (matchHash, versionHash, title);
        }

        private void Load(bool onlyMatchHash)
        {
            Task.Run(() => LoadAsync(CancellationToken.None)).ContinueInDispatcher(_dispatcher, getResult =>
            {
                try
                {
                    (_matchHash, var versionHash, _title) = getResult();
                    if (onlyMatchHash)
                    {
                        SetState(StorageModStateEnum.CreatedWithUpdateTarget, StorageModStateEnum.Loading);
                    }
                    else
                    {
                        _versionHash = versionHash;
                        SetState(StorageModStateEnum.Created, StorageModStateEnum.Loading);
                    }
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
            return _versionHash!;
        }

        public MatchHash GetMatchHash()
        {
            if (State == StorageModStateEnum.Loading) throw new InvalidOperationException($"Not allowed in State {State}");
            return _matchHash!;
        }

        public StorageModStateEnum GetState() => State;

        public string GetTitle() => _title ?? Identifier;

        public void Delete(bool removeData)
        {
            if (removeData) throw new NotImplementedException();
            IsDeleted = true;
        }

        public string GetAbsolutePath() => _implementation.Path;
        private UpdateTarget? UpdateTarget
        {
            get => _updateTarget;
            set
            {
                _updateTarget = value;
                _internalState.UpdateTarget = value;
            }
        }

        public event Action<IModelStorageMod>? StateChanged;

        public async Task<UpdateResult> Update(IRepositoryMod repositoryMod, MatchHash targetMatch,
            VersionHash targetVersion,
            IProgress<FileSyncStats>? progress, CancellationToken cancellationToken)
        {
            _logger.Trace("Progress: Waiting");
            progress?.Report(new FileSyncStats(FileSyncState.Waiting));
            _matchHash = targetMatch;
            _versionHash = targetVersion;
            UpdateTarget = new UpdateTarget(targetVersion.GetHashString());
            SetState(StorageModStateEnum.Updating, StorageModStateEnum.Created,
                StorageModStateEnum.CreatedWithUpdateTarget);

            void ReportProgress(FileSyncStats stats)
            {
                _logger.Trace($"Progress: {stats.State}");
                progress?.Report(stats);
            }

            cancellationToken.Register(() => ReportProgress(new FileSyncStats(FileSyncState.Stopping)));

            ReportProgress(new FileSyncStats(FileSyncState.Waiting));

            var result =
                await Task.Run(
                    async () => await RepoSync.UpdateAsync(repositoryMod, this, _implementation, cancellationToken,
                        progress), CancellationToken.None);

            ReportProgress(new FileSyncStats(FileSyncState.None));

            _dispatcher.ExecuteSynchronized(() =>
            {
                if (result == UpdateResult.Success)
                {
                    UpdateTarget = null;
                    SetState(StorageModStateEnum.Created, StorageModStateEnum.Updating);
                }
                else
                {
                    UpdateTarget = new UpdateTarget(targetVersion.GetHashString());
                    SetState(StorageModStateEnum.CreatedWithUpdateTarget, StorageModStateEnum.Updating);
                }
            });
            return result;
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
