using System;
using System.Collections.Generic;
using System.Threading;
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
        private readonly IModelStructure _modelStructure;
        private readonly Logger _logger = EntityLogger.GetLogger();
        public IRepositoryMod Implementation { get; } // TODO: make private
        public string Identifier { get; }
        public bool IsLoaded { get; private set; }
        public event Action OnLoaded;

        private readonly JobSlot _loading;

        private MatchHash _matchHash;
        private VersionHash _versionHash;

        public UpdateTarget AsUpdateTarget { get; private set; }

        private Exception _error;

        private RepositoryModActionSelection _selection;

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

        public RepositoryMod(IActionQueue actionQueue, IRepositoryMod implementation, string identifier,
            IJobManager jobManager, IPersistedRepositoryModState internalState,
            IModelStructure modelStructure)
        {
            _actionQueue = actionQueue;
            _jobManager = jobManager;
            _internalState = internalState;
            _modelStructure = modelStructure;
            Implementation = implementation;
            Identifier = identifier;
            DownloadIdentifier = identifier;

            // TODO: WTF.
            if (_internalState.Selection == PersistedSelection.Create(new RepositoryModActionSelection()))
                Selection = new RepositoryModActionSelection();

            _loading = new JobSlot(LoadInternal, $"Load RepoMod {Identifier}", _jobManager);

            _logger.Info($"Created with identifier {identifier}");
        }

        public void Load()
        {
            _loading.Request();
        }

        public string GetDisplayName()
        {
            if (!IsLoaded) throw new InvalidOperationException();
            return Implementation.GetDisplayName();
        }

        public void SignalAllStorageModsLoaded()
        {
            _logger.Info("all mods loaded.");
            DoAutoSelection();
        }

        private void DoAutoSelection()
        {
            // TODO: check if a better option became available and notify user
            // never change a selection once it was made. Would be clickjacking on the user
            if (Selection != null) return;

            _logger.Trace("Checking auto-selection for mod {0}", Identifier);

            var selection = CoreCalculation.AutoSelect(this, _modelStructure);
            Selection = selection;
        }

        public MatchHash GetMatchHash() => _matchHash;

        public VersionHash GetVersionHash() => _versionHash;

        private readonly HashSet<IModelStorageMod> _storageModStateChangedSubscriptions = new();

        public void ProcessMod(IModelStorageMod mod)
        {
            _logger.Info("added local mod.");
            if (_error != null) return;
            var actionType = CoreCalculation.GetModAction(this, mod);

            void Handler() => ProcessMod(mod);

            if (actionType == null)
            {
                if (!_storageModStateChangedSubscriptions.Contains(mod)) return;
                mod.StateChanged -= Handler;
                _storageModStateChangedSubscriptions.Remove(mod);
                return;
            }

            if (!_storageModStateChangedSubscriptions.Contains(mod))
            {
                mod.StateChanged += Handler;
                _storageModStateChangedSubscriptions.Add(mod);
            }

            _logger.Info("Set action for {0} to {1}", mod, actionType.ToString());

            if (actionType == ModActionEnum.LoadingMatch) return;

            LocalModUpdated?.Invoke(mod);

            if (mod.GetStorageModIdentifiers() == _internalState.Selection)
                Selection = new RepositoryModActionSelection(mod);

            DoAutoSelection();
        }

        private void LoadInternal(CancellationToken cancellationToken)
        {
            // TODO: use cancellationToken
            try
            {
                Implementation.Load();
                _matchHash = new MatchHash(Implementation);
                _versionHash = new VersionHash(Implementation);
                AsUpdateTarget = new UpdateTarget(_versionHash.GetHashString(), Implementation.GetDisplayName());
                _actionQueue.EnQueueAction(() =>
                {
                    IsLoaded = true;
                    OnLoaded?.Invoke();
                });
            }
            catch (Exception e)
            {
                _error = e;
                // TODO: should fire some event
            }
        }

        public IUpdateState DoUpdate()
        {
            if (!IsLoaded) throw new InvalidOperationException();
            if (Selection == null) throw new InvalidOperationException();

            // TODO: switch

            if (Selection.DoNothing) return null;

            if (Selection.StorageMod != null)
            {
                var action = CoreCalculation.GetModAction(this, Selection.StorageMod);
                if (action != ModActionEnum.Update && action != ModActionEnum.ContinueUpdate) return null;

                var update = Selection.StorageMod.PrepareUpdate(Implementation, AsUpdateTarget, _matchHash, _versionHash);
                return update;
            }

            if (Selection.DownloadStorage != null)
            {
                var update = Selection.DownloadStorage.PrepareDownload(Implementation, AsUpdateTarget, DownloadIdentifier, mod =>
                {
                    ProcessMod(mod);
                    Selection = new RepositoryModActionSelection(mod);
                }, _matchHash, _versionHash);
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
