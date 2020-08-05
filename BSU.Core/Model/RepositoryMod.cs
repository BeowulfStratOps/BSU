using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BSU.Core.Hashes;
using BSU.Core.JobManager;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class RepositoryMod
    {
        private readonly IInternalState _internalState;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        public Repository Repository { get; }
        public IRepositoryMod Implementation { get; }
        public string Identifier { get; }
        public Uid Uid { get; } = new Uid();

        private MatchHash _matchHash;
        private VersionHash _versionHash;

        private Exception _error;

        private readonly object _stateLock = new object(); // TODO: use it!!

        public StorageMod SelectedStorageMod { get; set; }
        public Storage SelectedDownloadStorage { get; set; }

        public Dictionary<StorageMod, ModAction> Actions { get; } = new Dictionary<StorageMod, ModAction>(); // TODO: wat is dis? Does it need a lock?

        private readonly JobSlot<SimpleJob> _loading;

        public RepositoryMod(Repository parent, IRepositoryMod implementation, string identifier, IJobManager jobManager, IInternalState internalState)
        {
            _internalState = internalState;
            Repository = parent;
            Implementation = implementation;
            Identifier = identifier;
            var title = $"Load RepoMod {Identifier}";
            _loading = new JobSlot<SimpleJob>(() => new SimpleJob(Load, title, 1), title, jobManager);
            /*_loading.OnFinished += error =>
            {
                _error = error; // TODO: error handling
            };*/
            
            _loading.StartJob();
        }

        private void Load(CancellationToken cancellationToken)
        {
            // TODO: use cancellationToken
            Implementation.Load();
            var match = new MatchHash(Implementation);
            var version = new VersionHash(Implementation);
            Repository.Model.EnQueueAction(() =>
            {
                _matchHash = match;
                _versionHash = version;
                StateChanged?.Invoke();
            });
        }

        public event Action StateChanged;

        public RepositoryModState GetState()
        {
            lock (_stateLock)
            {
                return new RepositoryModState(_matchHash, _versionHash, _error);                
            }
        }

        internal void ChangeAction(StorageMod target, ModActionEnum? newAction, bool allModsLoaded)
        {
            // TODO: signal if allModsLoaded becomes true. might be important for displaying things
            var existing = Actions.ContainsKey(target);
            if (newAction == null)
            {
                if (!existing) return;
                Actions[target].Remove();
                Actions.Remove(target);
                if (SelectedStorageMod == target)
                {
                    SelectedStorageMod = null;
                    SelectionChanged?.Invoke();
                }
                return;
            }
            if (existing)
            {
                Actions[target].Update((ModActionEnum) newAction);
            }
            else
            {
                Actions[target] = new ModAction(target, (ModActionEnum) newAction, this);
                ActionAdded?.Invoke(target);
            }
            DoAutoSelection(allModsLoaded);
            Repository.ReCalculateState();
        }

        public void NotifyAllModsLoaded()
        {
            DoAutoSelection(true);
        }

        private void DoAutoSelection(bool allModsLoaded)
        {
            // never change a selection once it was made. Could effectively be clickjacking on the user
            // TODO: check if a better option became available and notify user
            if (SelectedDownloadStorage != null || SelectedStorageMod != null) return;

            if (_internalState.HasUsedMod(this))
            {
                var storageMod = Actions.Keys.FirstOrDefault(mod => _internalState.IsUsedMod(this, mod));
                if (storageMod != null)
                {
                    SelectedStorageMod = storageMod;
                    SelectionChanged?.Invoke();
                }
            }
            
            // Still loading
            if (!allModsLoaded) return;
            if (Actions.Values.Any(action => action.ActionType == ModActionEnum.Loading)) return;
            
            // Order of precedence
            var precedence = new[]
                {ModActionEnum.Use, ModActionEnum.Await, ModActionEnum.ContinueUpdate, ModActionEnum.Update};

            foreach (var actionType in precedence)
            {
                var storageMod = Actions.Keys.FirstOrDefault(mod => Actions[mod].ActionType == actionType && !Actions[mod].Conflicts.Any());
                if (storageMod == null) continue;

                SelectedStorageMod = storageMod;
                SelectionChanged?.Invoke();
                return;
            }

            if (!Actions.Any())
            {
                var downloadStorage = Repository.Model.Storages.FirstOrDefault(storage => storage.Implementation.CanWrite());
                if (downloadStorage != null)
                {
                    SelectedDownloadStorage = downloadStorage;
                    SelectionChanged?.Invoke();
                }
            }
        }
        
        public event Action<StorageMod> ActionAdded;
        public event Action SelectionChanged; // TODO: use a property to call it
    }
}