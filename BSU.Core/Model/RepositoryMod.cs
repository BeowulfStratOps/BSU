using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Hashes;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.Core.Services;
using BSU.Core.Sync;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class RepositoryMod : IModelRepositoryMod
    {
        private readonly IPersistedRepositoryModState _internalState;
        private readonly ILogger _logger;
        private IRepositoryMod Implementation { get; } // TODO: make private
        public string Identifier { get; }
        public IModelRepository ParentRepository { get; }

        private MatchHash _matchHash;
        private VersionHash _versionHash;

        private RepositoryModActionSelection _selection;

        public event Action<IModelRepositoryMod> StateChanged;
        public PersistedSelection GetPreviousSelection() => _internalState.Selection;
        public event Action<IModelRepositoryMod> SelectionChanged;

        private RepositoryModActionSelection Selection
        {
            get => _selection;
            set
            {
                if (Equals(value, _selection)) return;
                _logger.Debug($"Changing selection from {_selection} to {value}");
                _selection = value;
                _internalState.Selection = PersistedSelection.FromSelection(value);
                SelectionChanged?.Invoke(this);
            }
        }

        public RepositoryMod(IRepositoryMod implementation, string identifier,
            IPersistedRepositoryModState internalState, IModelRepository parentRepository)
        {
            _logger = LogHelper.GetLoggerWithIdentifier(this, identifier);
            _internalState = internalState;
            ParentRepository = parentRepository;
            Implementation = implementation;
            Identifier = identifier;
            DownloadIdentifier = identifier;

            if (_internalState.Selection?.Type == PersistedSelectionType.DoNothing)
                _selection = new RepositoryModActionDoNothing();

            Load();
        }

        public LoadingState State
        {
            get => _state;
            private set
            {
                if (_state == value) return;
                _logger.Debug($"Changing state from {_state} to {value}");
                _state = value;
                StateChanged?.Invoke(this);
            }
        }

        private async Task<(MatchHash matchHash, VersionHash versionHash, ModInfo modInfo)> LoadAsync(CancellationToken cancellationToken)
        {
            var matchHash = await MatchHash.CreateAsync(Implementation, cancellationToken);
            var versionHash = await VersionHash.CreateAsync(Implementation, cancellationToken);
            var modInfo = await GetModInfo(cancellationToken);

            return (matchHash, versionHash, modInfo);
        }

        private void Load()
        {
            Task.Run(() => LoadAsync(CancellationToken.None)).ContinueInCurrentContext(getResult =>
            {
                (_matchHash, _versionHash, _modInfo) = getResult();
                State = LoadingState.Loaded;
            });
        }

        private ModInfo _modInfo;
        private async Task<ModInfo> GetModInfo(CancellationToken cancellationToken)
        {
            var (name, version) = await Implementation.GetDisplayInfo(cancellationToken);
            var size = 0UL;
            foreach (var file in await Implementation.GetFileList(cancellationToken))
            {
                size += await Implementation.GetFileSize(file, cancellationToken);
            }
            return new ModInfo(name, version, size);
        }

        public ModInfo GetModInfo()
        {
            if (State != LoadingState.Loaded) throw new InvalidOperationException();
            return _modInfo;
        }

        public MatchHash GetMatchHash()
        {
            if (State != LoadingState.Loaded) throw new InvalidOperationException();
            return _matchHash;
        }

        public VersionHash GetVersionHash()
        {
            if (State != LoadingState.Loaded) throw new InvalidOperationException();
            return _versionHash;
        }

        public void SetSelection(RepositoryModActionSelection selection)
        {
            Selection = selection;
        }

        public RepositoryModActionSelection GetCurrentSelection() => Selection;


        public async Task<IModUpdate> StartUpdate(IProgress<FileSyncStats> progress, CancellationToken cancellationToken)
        {
            if (State != LoadingState.Loaded) throw new InvalidOperationException();

            if (Selection == null) throw new InvalidOperationException();

            // TODO: switch

            if (Selection is RepositoryModActionDoNothing) return null;

            if (Selection is RepositoryModActionStorageMod actionStorageMod)
            {
                var action = CoreCalculation.GetModAction(this, actionStorageMod.StorageMod);
                if (action == ModActionEnum.AbortActiveAndUpdate) throw new NotImplementedException();
                if (action != ModActionEnum.Update && action != ModActionEnum.ContinueUpdate && action != ModActionEnum.AbortAndUpdate) return null;

                var update = actionStorageMod.StorageMod.PrepareUpdate(Implementation, _matchHash, _versionHash, progress);
                return update;
            }

            if (Selection is RepositoryModActionDownload actionDownload)
            {
                var updateTarget = new UpdateTarget(_versionHash.GetHashString());
                var mod = await actionDownload.DownloadStorage.CreateMod(DownloadIdentifier, updateTarget);
                Selection = new RepositoryModActionStorageMod(mod);
                var update = mod.PrepareUpdate(Implementation, _matchHash, _versionHash, progress);
                return update;
            }

            throw new InvalidOperationException();
        }

        private string _downloadIdentifier;
        private LoadingState _state;

        public string DownloadIdentifier
        {
            get => _downloadIdentifier;
            set
            {
                if (!value.StartsWith("@")) throw new ArgumentException();
                _downloadIdentifier = value;
            }
        }

        public override string ToString() => Identifier;
    }
}
