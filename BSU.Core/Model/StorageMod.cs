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
        private readonly IModelStructure _modelStructure;
        public bool CanWrite { get; }
        public string Identifier { get; }
        public IModelStorage ParentStorage { get; }
        public IStorageMod Implementation { get; }

        private readonly ResettableLazyAsync<MatchHash> _matchHash;
        private readonly ResettableLazyAsync<VersionHash> _versionHash;

        private UpdateTarget _updateTarget;

        private readonly ILogger _logger;

        private readonly SemaphoreSlim _stateLock = new(1);
        private StorageModStateEnum _state = StorageModStateEnum.Created; // TODO: should not be directly accessible

        private StorageModStateEnum State
        {
            get => _state;
            set
            {
                var old = _state;
                _state = value;
                _logger.Debug("State changed from {1} to {2}.", Identifier, old, value);
                StateChanged?.Invoke();
            }
        }

        public StorageMod(IStorageMod implementation, string identifier,
            IPersistedStorageModState internalState, IModelStorage parent, bool canWrite, IModelStructure modelStructure)
        {
            _logger = LogHelper.GetLoggerWithIdentifier(this, identifier);
            _internalState = internalState;
            _modelStructure = modelStructure;
            ParentStorage = parent;
            CanWrite = canWrite;
            Implementation = implementation;
            Identifier = identifier;

            VersionHash initialVersionHash = null;

            _updateTarget = _internalState.UpdateTarget;
            if (_updateTarget != null)
            {
                _state = StorageModStateEnum.CreatedWithUpdateTarget;
                initialVersionHash = VersionHash.FromDigest(_updateTarget.Hash);
            }

            _matchHash = new ResettableLazyAsync<MatchHash>(CreateMatchHash, null, evt => _logger.Debug($"MatchHash job: {evt}"));
            _versionHash = new ResettableLazyAsync<VersionHash>(CreateVersionHash, initialVersionHash, evt => _logger.Debug($"VersionHash job: {evt}"));
        }

        private async Task<VersionHash> CreateVersionHash(CancellationToken cancellationToken)
        {
            return await VersionHash.CreateAsync(Implementation, cancellationToken);
        }

        private async Task<MatchHash> CreateMatchHash(CancellationToken cancellationToken)
        {
            return await MatchHash.CreateAsync(Implementation, cancellationToken);
        }

        public async Task<VersionHash> GetVersionHash(CancellationToken cancellationToken) => await _versionHash.GetAsync(cancellationToken);

        public async Task<MatchHash> GetMatchHash(CancellationToken cancellationToken) => await _matchHash.GetAsync(cancellationToken);

        public StorageModStateEnum GetState() => State;
        public async Task<IEnumerable<IModelRepositoryMod>> GetUsedBy(CancellationToken cancellationToken)
        {
            var repositoryMods = await _modelStructure.GetRepositoryMods();
            var result = new List<IModelRepositoryMod>();
            foreach (var repositoryMod in repositoryMods)
            {
                var selection = await repositoryMod.GetSelection(cancellationToken);
                if (selection is RepositoryModActionStorageMod storageMod && storageMod.StorageMod == this)
                    result.Add(repositoryMod);
            }

            return result;
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

        public async Task<IModUpdate> PrepareUpdate(IRepositoryMod repositoryMod, string targetDisplayName, MatchHash targetMatch, VersionHash targetVersion, IProgress<FileSyncStats> progress)
        {
            _logger.Trace("Progress: Waiting");
            progress?.Report(new FileSyncStats(FileSyncState.Waiting));
            await SetState(StorageModStateEnum.Updating, new [] { StorageModStateEnum.Created , StorageModStateEnum.CreatedWithUpdateTarget},
                async () =>
                {
                    await _matchHash.Set(targetMatch);
                    await _versionHash.Set(targetVersion);
                    UpdateTarget = new UpdateTarget(targetVersion.GetHashString(), targetDisplayName);
                });

            var update = new StorageModUpdateState(this, repositoryMod, progress);

            update.OnEnded += async () => await SetState(StorageModStateEnum.Created, new[]
            {
                StorageModStateEnum.Updating
            });

            return update;
        }

        private async Task SetState(StorageModStateEnum newState, StorageModStateEnum[] acceptableCurrent, Func<Task> setValues = null)
        {
            await _stateLock.WaitAsync();
            // TODO: handle errors?
            try
            {
                if (!acceptableCurrent.Contains(State)) throw new InvalidOperationException($"Tried to transition from {State} to {newState}");
                await Task.WhenAll(_matchHash.ResetAndWaitAsync(), _versionHash.ResetAndWaitAsync());
                UpdateTarget = null;
                if (setValues != null) await setValues();
                State = newState;
            }
            finally
            {
                _stateLock.Release();
            }
        }

        public async Task Abort()
        {
            await SetState(StorageModStateEnum.Created, new[] { StorageModStateEnum.CreatedWithUpdateTarget });
        }

        public PersistedSelection GetStorageModIdentifiers()
        {
            return new PersistedSelection(PersistedSelectionType.StorageMod, ParentStorage.Identifier, Identifier);
        }

        public override string ToString() => Identifier;
    }
}
