using System;
using System.Collections.Generic;
using System.Linq;
using BSU.CoreCommon;

namespace BSU.Core.State
{
    public class State
    {
        public readonly List<Repo> Repos;
        public readonly List<Storage> Storages;
        internal readonly Core Core;
        public bool IsValid { get; private set; }

        public event Action Invalidated;

        internal State(IEnumerable<IRepository> repos, IEnumerable<IStorage> storages, Core core)
        {
            Core = core;
            core.StateInvalidated += InvalidateState; // TODO: does that mess with GC?
            Storages = storages.Select(s => new Storage(s, this)).ToList();
            Repos = repos.Select(r => new Repo(r, this)).ToList();
            foreach (var repo in Repos)
            {
                repo.CollectConflicts();
            }
        }

        private void InvalidateState()
        {
            IsValid = false;
            Invalidated?.Invoke();
        }
    }
}
