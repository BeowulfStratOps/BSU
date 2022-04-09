using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Events;
using BSU.Core.Hashes;
using BSU.Core.Ioc;
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
        private readonly IRepositoryMod _implementation;
        public string Identifier { get; }
        public IModelRepository ParentRepository { get; }

        private MatchHash? _matchHash;
        private VersionHash? _versionHash;

        private ModSelection _selection = new ModSelectionNone();

        public event Action<IModelRepositoryMod>? StateChanged;
        public PersistedSelection? GetPreviousSelection() => _internalState.Selection;
        public event Action<IModelRepositoryMod>? SelectionChanged;

        private ModSelection Selection
        {
            get => _selection;
            set
            {
                if (Equals(value, _selection)) return;
                _logger.Debug($"Changing selection from {_selection} to {value}");
                var old = _selection;
                _selection = value;
                _internalState.Selection = PersistedSelection.FromSelection(value);
                SelectionChanged?.Invoke(this);
                _eventManager.Publish(new ModSelectionChangedEvent(this, old, value));
            }
        }

        public RepositoryMod(IRepositoryMod implementation, string identifier,
            IPersistedRepositoryModState internalState, IModelRepository parentRepository, IServiceProvider services)
        {
            _logger = LogHelper.GetLoggerWithIdentifier(this, identifier);
            _internalState = internalState;
            ParentRepository = parentRepository;
            _dispatcher = services.Get<IDispatcher>();
            _eventManager = services.Get<IEventManager>();
            _implementation = implementation;
            Identifier = identifier;

            if (_internalState.Selection?.Type == PersistedSelectionType.DoNothing)
                _selection = new ModSelectionDisabled();

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
            var matchHash = await MatchHash.CreateAsync(_implementation, cancellationToken);
            var versionHash = await VersionHash.CreateAsync(_implementation, cancellationToken);
            var modInfo = await GetModInfo(cancellationToken);

            return (matchHash, versionHash, modInfo);
        }

        private void Load()
        {
            Task.Run(() => LoadAsync(CancellationToken.None)).ContinueInDispatcher(_dispatcher, getResult =>
            {
                // TODO: handle errors!
                (_matchHash, _versionHash, _modInfo) = getResult();
                State = LoadingState.Loaded;
            });
        }

        private ModInfo? _modInfo;
        private async Task<ModInfo> GetModInfo(CancellationToken cancellationToken)
        {
            var (name, version) = await _implementation.GetDisplayInfo(cancellationToken);
            var size = 0UL;
            foreach (var file in await _implementation.GetFileList(cancellationToken))
            {
                size += await _implementation.GetFileSize(file, cancellationToken);
            }
            return new ModInfo(name, version, size);
        }

        public ModInfo GetModInfo()
        {
            if (State != LoadingState.Loaded) throw new InvalidOperationException($"Nor allowed in state {State}");
            return _modInfo!;
        }

        public MatchHash GetMatchHash()
        {
            if (State != LoadingState.Loaded) throw new InvalidOperationException($"Not allowed in State {State}");
            return _matchHash!;
        }

        public VersionHash GetVersionHash()
        {
            if (State != LoadingState.Loaded) throw new InvalidOperationException($"Not allowed in State {State}");
            return _versionHash!;
        }

        public void SetSelection(ModSelection selection)
        {
            Selection = selection;
        }

        public ModSelection GetCurrentSelection() => Selection;


        public async Task<ModUpdateInfo?> StartUpdate(IProgress<FileSyncStats>? progress, CancellationToken cancellationToken)
        {
            if (State != LoadingState.Loaded) throw new InvalidOperationException($"Not allowed in State {State}");

            if (Selection == null) throw new InvalidOperationException("Can't update if the selection is null");

            // TODO: switch

            if (Selection is ModSelectionDisabled) return null;

            if (Selection is ModSelectionStorageMod actionStorageMod)
            {
                var storageMod = actionStorageMod.StorageMod;
                var action = CoreCalculation.GetModAction(this, storageMod);
                if (action == ModActionEnum.AbortActiveAndUpdate) throw new NotImplementedException();
                if (action != ModActionEnum.Update && action != ModActionEnum.ContinueUpdate && action != ModActionEnum.AbortAndUpdate) return null;

                var updateTask = storageMod.Update(_implementation, _matchHash!, _versionHash!, progress, cancellationToken);
                return new ModUpdateInfo(updateTask, storageMod);
            }

            if (Selection is ModSelectionDownload actionDownload)
            {
                var updateTarget = new UpdateTarget(_versionHash!.GetHashString());
                var mod = await actionDownload.DownloadStorage.CreateMod(actionDownload.DownloadName, updateTarget, _matchHash!);
                Selection = new ModSelectionStorageMod(mod);
                var updateTask = mod.Update(_implementation, _matchHash!, _versionHash, progress, cancellationToken);
                return new ModUpdateInfo(updateTask, mod);
            }

            throw new InvalidOperationException(); // Impossible
        }

        private LoadingState _state;
        private readonly IDispatcher _dispatcher;
        private readonly IEventManager _eventManager;

        public override string ToString() => Identifier;
    }
}
