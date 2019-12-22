using System;
using System.Collections.Generic;
using System.Linq;
using BSU.CoreCommon;
using NLog;
using NLog.Fluent;

namespace BSU.Core.State
{
    public class State
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public readonly List<Repo> Repos;
        public readonly List<Storage> Storages;
        internal readonly Core Core;
        public bool IsValid { get; private set; }

        internal readonly Uid Uid = new Uid();

        public event Action Invalidated;

        internal State(IEnumerable<IRepository> repos, IEnumerable<IStorage> storages, Core core)
        {
            Logger.Debug("Creating new state {0}", Uid);
            Core = core;
            core.StateInvalidated += InvalidateState; // TODO: does that mess with GC?
            Logger.Debug("Creating storage states");
            Storages = storages.Select(s => new Storage(s, this)).ToList();
            Logger.Debug("Creating repository states");
            Repos = repos.Select(r => new Repo(r, this)).ToList();
            foreach (var repo in Repos)
            {
                Logger.Debug("Collecting conflicts in repo {0}", repo.Uid);
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
