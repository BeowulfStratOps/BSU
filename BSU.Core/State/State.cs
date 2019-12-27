using System;
using System.Collections.Generic;
using System.Linq;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.State
{
    public class State
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public readonly IReadOnlyList<Repository> Repos;
        public readonly IReadOnlyList<Storage> Storages;
        internal readonly Core Core;
        public bool IsValid { get; private set; } = true;

        internal readonly Uid Uid = new Uid();

        public event Action Invalidated;

        internal State(IEnumerable<IRepository> repos, IEnumerable<IStorage> storages, Core core)
        {
            Logger.Debug("Creating new state {0}", Uid);
            Core = core;
            core.StateInvalidated += InvalidateState; // TODO: this messes with GC
            Logger.Debug("Creating storage states");
            Storages = storages.Select(s => new Storage(s, this)).ToList().AsReadOnly();
            Logger.Debug("Creating repository states");
            Repos = repos.Select(r => new Repository(r, this)).ToList().AsReadOnly();
            foreach (var repo in Repos)
            {
                Logger.Debug("Collecting conflicts in repo {0}", repo.Uid);
                repo.CollectConflicts();
            }
        }

        internal void InvalidateState()
        {
            IsValid = false;
            Invalidated?.Invoke();
        }
    }
}