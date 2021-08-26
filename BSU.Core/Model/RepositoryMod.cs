using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Hashes;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class RepositoryMod : IModelRepositoryMod
    {
        private readonly IPersistedRepositoryModState _internalState;
        private readonly IModelStructure _modelStructure;
        private readonly Logger _logger = EntityLogger.GetLogger();
        private IRepositoryMod Implementation { get; } // TODO: make private
        public string Identifier { get; }

        private readonly ResettableLazyAsync<MatchHash> _matchHash;
        private readonly ResettableLazyAsync<VersionHash> _versionHash;

        private RepositoryModActionSelection _selection;

        private RepositoryModActionSelection Selection
        {
            get => _selection;
            set
            {
                if (value == _selection) return;
                _logger.Debug("Mod {0} changing selection from {1} to {2}", Identifier, _selection, value);
                _selection = value;
                _internalState.Selection = PersistedSelection.Create(value);
            }
        }

        public RepositoryMod(IRepositoryMod implementation, string identifier,
            IPersistedRepositoryModState internalState,
            IModelStructure modelStructure)
        {
            _internalState = internalState;
            _modelStructure = modelStructure;
            Implementation = implementation;
            Identifier = identifier;
            DownloadIdentifier = identifier;

            // TODO: WTF.
            if (_internalState.Selection == PersistedSelection.Create(new RepositoryModActionSelection()))
                Selection = new RepositoryModActionSelection();

            // TODO: is there a cleaner way of doing caching?
            _matchHash = new ResettableLazyAsync<MatchHash>(CalculateMatchHash, null, CancellationToken.None);
            _versionHash = new ResettableLazyAsync<VersionHash>(CalculateVersionHash, null, CancellationToken.None);

            _logger.Info($"Created with identifier {identifier}");
        }

        private async Task<MatchHash> CalculateMatchHash(CancellationToken cancellationToken)
        {
            return await MatchHash.CreateAsync(Implementation, cancellationToken);
        }

        private async Task<VersionHash> CalculateVersionHash(CancellationToken cancellationToken)
        {
            return await VersionHash.CreateAsync(Implementation, cancellationToken);
        }

        public async Task<string> GetDisplayName(CancellationToken cancellationToken)
        {
            // TODO: cache
            return await Implementation.GetDisplayName(cancellationToken);
        }

        public async Task<MatchHash> GetMatchHash(CancellationToken cancellationToken) => await _matchHash.Get();

        public async Task<VersionHash> GetVersionHash(CancellationToken cancellationToken) => await _versionHash.Get();

        public void SetSelection(RepositoryModActionSelection selection)
        {
            Selection = selection;
        }

        public async Task<RepositoryModActionSelection> GetSelection(CancellationToken cancellationToken)
        {
            // TODO: check if a better option became available and notify user ??

            // never change a selection once it was made. Would be clickjacking on the user
            if (Selection != null) return Selection;

            _logger.Trace("Checking auto-selection for mod {0}", Identifier);

            // TODO: check our selected one (well.. initially selected one. should be a separate field/flag)

            var storageMods = await _modelStructure.GetStorageMods();
            var (result, selectedMod) = await CoreCalculation.AutoSelect(this, storageMods, cancellationToken);
            var selection = result switch
            {
                AutoSelectResult.Success => new RepositoryModActionSelection(selectedMod),
                AutoSelectResult.Download => new RepositoryModActionSelection(_modelStructure.GetStorages().First()),
                AutoSelectResult.None => null,
                _ => throw new ArgumentOutOfRangeException()
            };
            Selection = selection;
            return selection;
        }

        public async Task<List<IModelRepositoryMod>> GetConflicts(CancellationToken cancellationToken)
        {
            var result = new List<IModelRepositoryMod>();
            var selection = await GetSelection(cancellationToken);
            if (selection.StorageMod == null) return result;
            var otherMods = await _modelStructure.GetRepositoryMods();

            // TODO: do in parallel?
            foreach (var mod in otherMods)
            {
                if (mod == this) continue;
                if (await CoreCalculation.IsConflicting(this, mod, selection.StorageMod, cancellationToken))
                    result.Add(mod);
            }

            return result;
        }

        public async Task<IUpdateCreated> StartUpdate(CancellationToken cancellationToken)
        {
            if (Selection == null) throw new InvalidOperationException();

            // TODO: switch

            if (Selection.DoNothing) return null;

            var matchHash = await _matchHash.Get();
            var versionHash = await _versionHash.Get();
            var displayName = await GetDisplayName(cancellationToken);

            if (Selection.StorageMod != null)
            {
                var action = await CoreCalculation.GetModAction(this, Selection.StorageMod, cancellationToken);
                if (action != ModActionEnum.Update && action != ModActionEnum.ContinueUpdate) return null;

                var update = await Selection.StorageMod.PrepareUpdate(Implementation, displayName, matchHash, versionHash);
                return update;
            }

            if (Selection.DownloadStorage != null)
            {
                var updateTarget = new UpdateTarget(versionHash.GetHashString(), displayName);
                var mod = await Selection.DownloadStorage.CreateMod(DownloadIdentifier, updateTarget);
                Selection = new RepositoryModActionSelection(mod);
                var update = await mod.PrepareUpdate(Implementation, displayName, matchHash, versionHash);
                return update;
            }

            throw new InvalidOperationException();
        }

        public string DownloadIdentifier { get; set; }

        public override string ToString() => Identifier;
    }
}
