using System;
using System.Collections.Generic;
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
            _loading.OnFinished += error =>
            {
                _error = error;
                StateChanged?.Invoke();
            };
            
            _loading.StartJob();
        }

        private void Load(CancellationToken cancellationToken)
        {
            // TODO: use cancellationToken
            Implementation.Load();
            _matchHash = new MatchHash(Implementation);
            _versionHash = new VersionHash(Implementation);
        }

        public event Action StateChanged;

        public RepositoryModState GetState()
        {
            lock (_stateLock)
            {
                return new RepositoryModState(_matchHash, _versionHash, _error);                
            }
        }

        internal void ChangeAction(StorageMod target, ModActionEnum? newAction)
        {
            var existing = Actions.ContainsKey(target);
            if (newAction == null)
            {
                if (!existing) return;
                Actions[target].Remove();
                Actions.Remove(target);
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
        }
        public event Action<StorageMod> ActionAdded;
    }
}