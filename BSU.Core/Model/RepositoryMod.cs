using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.JobManager;
using BSU.Core.Persistence;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class RepositoryMod : IModelRepositoryMod
    {
        private readonly IActionQueue _actionQueue;
        private readonly IJobManager _jobManager;
        private readonly IPersistedRepositoryModState _internalState;
        private readonly RelatedActionsBag _relatedActionsBag;
        private readonly IModelStructure _modelStructure;
        private readonly Logger _logger = EntityLogger.GetLogger();
        public IRepositoryMod Implementation { get; } // TODO: make private
        public string Identifier { get; }

        private readonly JobSlot _loading;

        private MatchHash _matchHash;
        private VersionHash _versionHash;

        public UpdateTarget AsUpdateTarget { get; private set; }

        private Exception _error;

        private RepositoryModActionSelection _selection;

        public event Action OnUpdateChange;

        public RepositoryModActionSelection Selection
        {
            get => _selection;
            set
            {
                if (value == _selection) return;
                _logger.Debug("Mod {0} changing selection from {1} to {2}", Identifier, _selection, value);
                _selection = value;
                _internalState.Selection = PersistedSelection.Create(value);
                SelectionChanged?.Invoke();
            }
        }

        public Dictionary<IModelStorageMod, ModAction> LocalMods { get; } = new Dictionary<IModelStorageMod, ModAction>();

        public RepositoryMod(IActionQueue actionQueue, IRepositoryMod implementation, string identifier,
            IJobManager jobManager, IPersistedRepositoryModState internalState, RelatedActionsBag relatedActionsBag,
            IModelStructure modelStructure)
        {
            _actionQueue = actionQueue;
            _jobManager = jobManager;
            _internalState = internalState;
            _relatedActionsBag = relatedActionsBag;
            _modelStructure = modelStructure;
            Implementation = implementation;
            Identifier = identifier;
            DownloadIdentifier = identifier;

            _loading = new JobSlot(LoadInternal, $"Load RepoMod {Identifier}", _jobManager);

            _logger.Info($"Created with iddentifier {identifier}");
        }

        private async Task Load()
        {
            if (_error != null) return;
            try
            {
                await _loading.Do();
            }
            catch (Exception e)
            {
                _error = e;
            }
        }

        public async Task<string> GetDisplayName()
        {
            await Load();
            return Implementation.GetDisplayName();
        }

        public async Task SignalAllStorageModsLoaded()
        {
            _logger.Info("all mods loaded.");
            // never change a selection once it was made. Would be clickjacking on the user
            // TODO: check if a better option became available and notify user (must only ever happen for old selections)
            if (Selection != null) return;

            _logger.Trace("Checking auto-selection for mod {0}", Identifier);

            var selection = await CoreCalculation.AutoSelect(LocalMods, _modelStructure);
            if (selection == Selection) return;

            _logger.Trace("Auto-selection for mod {0} changed to {1}", Identifier, selection);

            Selection = selection;
        }

        public async Task ProcessMod(IModelStorageMod mod)
        {
            _logger.Info("added local mod.");
            await Load();
            if (_error != null) return;
            var actionType = await CoreCalculation.GetModAction(_matchHash, _versionHash, mod);
            if (actionType == null) return;

            if (!LocalMods.ContainsKey(mod))
                mod.StateChanged += async () => await ProcessMod(mod); // TODO: potential memory leak
            LocalMods[mod] = new ModAction((ModActionEnum) actionType, this, _versionHash,
                _relatedActionsBag.GetBag(mod));
            LocalModUpdated?.Invoke(mod);
            if (mod.GetStorageModIdentifiers() == _internalState.Selection)
                Selection = new RepositoryModActionSelection(mod);
        }

        private void LoadInternal(CancellationToken cancellationToken)
    {
            // TODO: use cancellationToken
            Implementation.Load();
            _matchHash = new MatchHash(Implementation);
            _versionHash = new VersionHash(Implementation);
            AsUpdateTarget = new UpdateTarget(_versionHash.GetHashString(), Implementation.GetDisplayName());
        }

        public IUpdateState DoUpdate()
        {
            if (Selection == null) throw new InvalidOperationException();

            // TODO: switch

            if (Selection.DoNothing) return null;

            if (Selection.StorageMod != null)
            {
                var action = LocalMods[Selection.StorageMod];
                if (action.ActionType != ModActionEnum.Update && action.ActionType != ModActionEnum.ContinueUpdate) return null;

                var update = Selection.StorageMod.PrepareUpdate(Implementation, AsUpdateTarget, _matchHash, _versionHash);
                return update;
            }

            if (Selection.DownloadStorage != null)
            {
                var update = Selection.DownloadStorage.PrepareDownload(Implementation, AsUpdateTarget, DownloadIdentifier, async mod =>
                {
                    await ProcessMod(mod);
                    Selection = new RepositoryModActionSelection(mod);
                });
                return update;
            }

            throw new InvalidOperationException();
        }

        public string DownloadIdentifier { get; set; }

        public override string ToString() => Identifier;

        public event Action<IModelStorageMod> LocalModUpdated;
        public event Action SelectionChanged;
    }
}
