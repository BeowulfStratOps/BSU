using System;
using System.Collections.Generic;
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
using BSU.CoreCommon.Hashes;
using BSU.Hashes;
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

        private HashCollection _hashes = null!;

        private ModSelection _selection = new ModSelectionNone();

        public event Action<IModelRepositoryMod>? StateChanged;

        private PersistedSelection? _previousSelection;
        public PersistedSelection? GetPreviousSelection() => _previousSelection;
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
            _jobManager = services.Get<IJobManager>();
            _eventManager = services.Get<IEventManager>();
            _implementation = implementation;
            _modActionService = services.Get<IModActionService>();
            Identifier = identifier;

            if (_internalState.Selection?.Type == PersistedSelectionType.DoNothing)
                _selection = new ModSelectionDisabled();
            else
                _previousSelection = _internalState.Selection;

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

        private async Task<(HashCollection, ModInfo modInfo)> LoadAsync(CancellationToken cancellationToken)
        {
            var hashes = await _implementation.GetHashes(cancellationToken);
            var modInfo = await GetModInfo(cancellationToken);

            return (hashes, modInfo);
        }

        private void Load()
        {
            _jobManager.Run(() => LoadAsync(CancellationToken.None), getResult =>
            {
                try
                {
                    (_hashes, _modInfo) = getResult();
                    State = LoadingState.Loaded;
                }
                catch (Exception e)
                {
                    // TODO: not shown in gui atm -> should show up as errored, but no notification
                    _logger.Error(e);
                    State = LoadingState.Error;
                }
            }, CancellationToken.None);
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

        public Task<IModHash> GetHash(Type type) => _hashes.GetHash(type);

        public List<Type> GetSupportedHashTypes() => _hashes.GetSupportedHashTypes();

        public void SetSelection(ModSelection selection)
        {
            _previousSelection = null;
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
                var action = _modActionService.GetModAction(this, storageMod);
                if (action == ModActionEnum.AbortActiveAndUpdate) throw new NotImplementedException();
                if (action != ModActionEnum.Update && action != ModActionEnum.ContinueUpdate && action != ModActionEnum.AbortAndUpdate) return null;

                var updateTarget = new UpdateTarget(_hashes, storageMod.Identifier);
                var updateTask = storageMod.Update(_implementation, updateTarget, progress, cancellationToken);
                return new ModUpdateInfo(updateTask, storageMod);
            }

            if (Selection is ModSelectionDownload actionDownload)
            {
                var mod = await actionDownload.DownloadStorage.CreateMod(actionDownload.DownloadName, _hashes);
                Selection = new ModSelectionStorageMod(mod);
                var target = new UpdateTarget(_hashes, actionDownload.DownloadName);
                var updateTask = mod.Update(_implementation, target, progress, cancellationToken);
                return new ModUpdateInfo(updateTask, mod);
            }

            throw new InvalidOperationException(); // Impossible
        }

        private LoadingState _state;
        private readonly IEventManager _eventManager;
        private readonly IModActionService _modActionService;
        private readonly IJobManager _jobManager;

        public override string ToString() => Identifier;
    }
}
