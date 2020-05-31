using System;
using System.Collections.Generic;
using BSU.Core.Hashes;
using BSU.Core.JobManager;
using SimpleJob = BSU.Core.View.SimpleJob; // TODO: WTF?? This should be a simple job ayyy lmao
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal class RepositoryMod
    {
        public Repository Repository { get; }
        public IRepositoryMod Implementation { get; }
        public string Identifier { get; }
        public Uid Uid { get; } = new Uid();

        private MatchHash _matchHash;
        private VersionHash _versionHash;

        private readonly object _stateLock = new object(); // TODO: use it!!

        public Dictionary<StorageMod, ModAction> Actions { get; } = new Dictionary<StorageMod, ModAction>(); // TODO: wat is dis? Does it need a lock?

        private readonly JobSlot<SimpleJob> _loading;

        public RepositoryMod(Repository parent, IRepositoryMod implementation, string identifier, IJobManager jobManager)
        {
            Repository = parent;
            Implementation = implementation;
            Identifier = identifier;
            var title = $"Load RepoMod {Identifier}";
            _loading = new JobSlot<SimpleJob>(() => new SimpleJob(Load, title, 1), title, jobManager);
            _loading.OnFinished += () => StateChanged?.Invoke();
            _loading.StartJob();
        }

        private void Load()
        {
            Implementation.Load();
            _matchHash = new MatchHash(Implementation);
            _versionHash = new VersionHash(Implementation);
        }

        public event Action StateChanged;

        public RepositoryModState GetState()
        {
            lock (_stateLock)
            {
                return new RepositoryModState(_matchHash, _versionHash);                
            }
        }

        internal void ChangeAction(StorageMod target, ModAction? newAction)
        {
            var existing = Actions.ContainsKey(target);
            if (newAction == null)
            {
                if (existing) Actions.Remove(target);
                return;
            }
            Actions[target] = (ModAction) newAction;
            if (existing)
                ActionChanged?.Invoke(target);
            else
                ActionAdded?.Invoke(target);
        }
        
        // TODO: ???
        public event Action<StorageMod> ActionAdded;
        public event Action<StorageMod> ActionChanged;
    }
}