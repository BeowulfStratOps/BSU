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
        private readonly IRepositoryModState _internalState;
        private readonly RelatedActionsBag _relatedActionsBag;
        private readonly IModelStructure _modelStructure;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public IRepositoryMod Implementation { get; }
        public string Identifier { get; }
        public Uid Uid { get; } = new Uid();

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
                _selection = value;
                SelectionChanged?.Invoke();
            }
        }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public Dictionary<IModelStorageMod, ModAction> Actions { get; } = new Dictionary<IModelStorageMod, ModAction>();

        private readonly JobSlot<SimpleJob> _loading;

        public RepositoryMod(IActionQueue actionQueue, IRepositoryMod implementation, string identifier,
            IJobManager jobManager, IRepositoryModState internalState, RelatedActionsBag relatedActionsBag,
            IModelStructure modelStructure)
        {
            _actionQueue = actionQueue;
            _internalState = internalState;
            _relatedActionsBag = relatedActionsBag;
            _modelStructure = modelStructure;
            Implementation = implementation;
            Identifier = identifier;
            var title = $"Load RepoMod {Identifier}";
            _loading = new JobSlot<SimpleJob>(() => new SimpleJob(Load, title, 1), title, jobManager);
            _loading.OnFinished += error =>
            {
                if (error == null) return;
                actionQueue.EnQueueAction(() =>
                {
                    _error = error;
                });
            };

            _loading.StartJob();
        }

        private void Load(CancellationToken cancellationToken)
        {
            // TODO: use cancellationToken
            Implementation.Load();
            var match = new MatchHash(Implementation);
            var version = new VersionHash(Implementation);
            _actionQueue.EnQueueAction(() =>
            {
                _matchHash = match;
                _versionHash = version;
                AsUpdateTarget =
                    new UpdateTarget(GetState().VersionHash.GetHashString(), Implementation.GetDisplayName());
                StateChanged?.Invoke();
            });
        }

        public event Action StateChanged;

        public RepositoryModState GetState()
        {
            return new RepositoryModState(_matchHash, _versionHash, _error);
        }

        public void ChangeAction(IModelStorageMod target, ModActionEnum? newAction)
        {
            _logger.Trace("RepoMod {0} changed action for {1} to {2}", Identifier, target, newAction);
            
            var existing = Actions.ContainsKey(target);
            if (newAction == null)
            {
                if (!existing) return;
                Actions[target].Remove();
                Actions.Remove(target);
                if (Selection?.StorageMod == target)
                {
                    Selection = null;
                }
                return;
            }
            if (existing)
            {
                Actions[target].Update((ModActionEnum) newAction);
            }
            else
            {
                Actions[target] = new ModAction((ModActionEnum) newAction, this, _versionHash, _relatedActionsBag.GetBag(target));
                ActionAdded?.Invoke(target);
            }
            DoAutoSelection();
        }

        private bool _allModsLoaded;
        
        public bool AllModsLoaded
        {
            private get => _allModsLoaded;
            set
            {
                if (value == _allModsLoaded) return;
                _allModsLoaded = value;
                _logger.Trace("Mod {0}: AllModsLoaded changed to {1}", Identifier, value);
                if (_allModsLoaded) DoAutoSelection();
            }
        }

        private void DoAutoSelection()
        {
            // never change a selection once it was made. Could effectively be clickjacking on the user
            // TODO: check if a better option became available and notify user
            if (Selection != null) return;

            _logger.Trace("Checking auto-selection for mod {0}", Identifier);
            
            var selection = CoreCalculation.AutoSelect(AllModsLoaded, Actions, _modelStructure, _internalState.UsedMod);
            if (selection == Selection) return;
            
            _logger.Trace("Auto-selection for mod {0} changed to {1}", Identifier, selection);
            
            Selection = selection;
        }

        public override string ToString() => Identifier;

        public event Action<IModelStorageMod> ActionAdded;
        public event Action SelectionChanged;
    }
}