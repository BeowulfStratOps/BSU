﻿using System;
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
    internal class RepositoryMod : IModelRepositoryMod
    {
        private readonly IPersistedRepositoryModState _internalState;
        private readonly IModelStructure _modelStructure;
        private readonly ILogger _logger;
        private IRepositoryMod Implementation { get; } // TODO: make private
        public string Identifier { get; }
        public IModelRepository ParentRepository { get; }

        private readonly ResettableLazyAsync<MatchHash> _matchHash;
        private readonly ResettableLazyAsync<VersionHash> _versionHash;

        private RepositoryModActionSelection _selection;

        private RepositoryModActionSelection Selection
        {
            get => _selection;
            set
            {
                if (value != null && value.Equals(_selection)) return;
                _logger.Debug($"Changing selection from {_selection} to {value}");
                _selection = value;
                _internalState.Selection = PersistedSelection.FromSelection(value);
            }
        }

        public RepositoryMod(IRepositoryMod implementation, string identifier,
            IPersistedRepositoryModState internalState,
            IModelStructure modelStructure, IModelRepository parentRepository)
        {
            _logger = LogHelper.GetLoggerWithIdentifier(this, identifier);
            _internalState = internalState;
            _modelStructure = modelStructure;
            ParentRepository = parentRepository;
            Implementation = implementation;
            Identifier = identifier;
            DownloadIdentifier = identifier;

            if (_internalState.Selection?.Type == PersistedSelectionType.DoNothing)
                _selection = new RepositoryModActionDoNothing();

            // TODO: is there a cleaner way of doing caching?
            _matchHash = new ResettableLazyAsync<MatchHash>(CalculateMatchHash, null);
            _versionHash = new ResettableLazyAsync<VersionHash>(CalculateVersionHash, null);
        }

        private async Task<MatchHash> CalculateMatchHash(CancellationToken cancellationToken)
        {
            return await MatchHash.CreateAsync(Implementation, cancellationToken);
        }

        private async Task<VersionHash> CalculateVersionHash(CancellationToken cancellationToken)
        {
            return await VersionHash.CreateAsync(Implementation, cancellationToken);
        }

        private ModInfo _modInfo;
        public async Task<ModInfo> GetModInfo(CancellationToken cancellationToken)
        {
            if (_modInfo != null) return _modInfo;
            var (name, version) = await Implementation.GetDisplayInfo(cancellationToken);
            var size = 0UL;
            foreach (var file in await Implementation.GetFileList(cancellationToken))
            {
                size += await Implementation.GetFileSize(file, cancellationToken);
            }
            _modInfo = new ModInfo(name, version, size);
            return _modInfo;
        }

        public async Task<MatchHash> GetMatchHash(CancellationToken cancellationToken) => await _matchHash.GetAsync(cancellationToken);

        public async Task<VersionHash> GetVersionHash(CancellationToken cancellationToken) => await _versionHash.GetAsync(cancellationToken);

        public void SetSelection(RepositoryModActionSelection selection)
        {
            Selection = selection;
        }

        public async Task<ModActionEnum> GetActionForMod(IModelStorageMod storageMod,
            CancellationToken cancellationToken)
        {
            return await CoreCalculation.GetModAction(this, storageMod, cancellationToken);
        }

        public async Task<List<(IModelStorageMod mod, ModActionEnum action)>> GetModActions(CancellationToken cancellationToken)
        {
            var mods = await _modelStructure.GetStorageMods();
            return (await mods.SelectAsync(async m => (m, await GetActionForMod(m, cancellationToken)))).ToList();
        }

        public async Task<RepositoryModActionSelection> GetSelection(bool reset = false, CancellationToken cancellationToken = default)
        {
            // never change a selection once it was made. Would be clickjacking on the user
            // exceptions are a forced reset, or the current selection not being available anymore
            if (Selection != null && !reset)
            {
                var deleted = Selection is RepositoryModActionDownload download && download.DownloadStorage.IsDeleted ||
                              Selection is RepositoryModActionStorageMod storageMod && storageMod.StorageMod.IsDeleted;
                if (!deleted) return Selection;
            }

            _logger.Trace("Checking auto-selection for mod {0}", Identifier);

            var storageMods = (await _modelStructure.GetStorageMods()).ToList();

            var previouslySelectedMod =
                storageMods.SingleOrDefault(m => m.GetStorageModIdentifiers().Equals(_internalState.Selection));

            if (previouslySelectedMod != null)
            {
                Selection = new RepositoryModActionStorageMod(previouslySelectedMod);
                return Selection;
            }

            // TODO: check previously selected storage for download?

            var selectedMod = await CoreCalculation.AutoSelect(this, storageMods, cancellationToken);
            RepositoryModActionSelection selection = null;

            if (selectedMod != null)
            {
                selection = new RepositoryModActionStorageMod(selectedMod);
            }
            else
            {
                var storage = (await _modelStructure.GetStorages().WhereAsync(async s => s.CanWrite && await s.IsAvailable())).FirstOrDefault();
                if (storage != null)
                {
                    selection = new RepositoryModActionDownload(storage);
                    DownloadIdentifier = await storage.GetAvailableDownloadIdentifier(Identifier);
                }
            }

            Selection = selection;
            return selection;
        }

        public async Task<List<IModelRepositoryMod>> GetConflicts(CancellationToken cancellationToken)
        {
            var selection = await GetSelection(cancellationToken: cancellationToken);
            if (selection is not RepositoryModActionStorageMod actionStorageMod) return new List<IModelRepositoryMod>();
            return await GetConflictsUsingMod(actionStorageMod.StorageMod, cancellationToken);
        }

        public async Task<List<IModelRepositoryMod>> GetConflictsUsingMod(IModelStorageMod storageMod, CancellationToken cancellationToken)
        {
            var result = new List<IModelRepositoryMod>();
            var otherMods = await _modelStructure.GetRepositoryMods();

            // TODO: do in parallel?
            foreach (var mod in otherMods)
            {
                if (mod == this) continue;
                if (await CoreCalculation.IsConflicting(this, mod, storageMod, cancellationToken))
                    result.Add(mod);
            }

            return result;
        }

        public async Task<IModUpdate> StartUpdate(IProgress<FileSyncStats> progress, CancellationToken cancellationToken)
        {
            if (Selection == null) throw new InvalidOperationException();

            // TODO: switch

            if (Selection is RepositoryModActionDoNothing) return null;

            // TODO: all at the same time
            var matchHash = await _matchHash.GetAsync(cancellationToken);
            var versionHash = await _versionHash.GetAsync(cancellationToken);
            var displayName = await GetModInfo(cancellationToken);

            if (Selection is RepositoryModActionStorageMod actionStorageMod)
            {
                var action = await CoreCalculation.GetModAction(this, actionStorageMod.StorageMod, cancellationToken);
                if (action == ModActionEnum.AbortActiveAndUpdate) throw new NotImplementedException();
                if (action != ModActionEnum.Update && action != ModActionEnum.ContinueUpdate && action != ModActionEnum.AbortAndUpdate) return null;

                var update = await actionStorageMod.StorageMod.PrepareUpdate(Implementation, matchHash, versionHash, progress);
                return update;
            }

            if (Selection is RepositoryModActionDownload actionDownload)
            {
                var updateTarget = new UpdateTarget(versionHash.GetHashString());
                var mod = await actionDownload.DownloadStorage.CreateMod("@" + DownloadIdentifier, updateTarget);
                Selection = new RepositoryModActionStorageMod(mod);
                var update = await mod.PrepareUpdate(Implementation, matchHash, versionHash, progress);
                return update;
            }

            throw new InvalidOperationException();
        }

        public string DownloadIdentifier { get; set; }

        public override string ToString() => Identifier;
    }
}
