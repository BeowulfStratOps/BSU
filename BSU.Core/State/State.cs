using System;
using System.Collections.Generic;
using System.Linq;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.State
{
    /// <summary>
    /// A time slice state of all repositories and storages the core is currently aware of. Any changes to data,
    /// or actions that imply change, will invalidate this instance.
    /// </summary>
    public class State
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public readonly IReadOnlyList<Repository> Repos;
        public readonly IReadOnlyList<Storage> Storages;
        internal readonly Core Core;
        public bool IsValid { get; private set; } = true;

        private readonly Uid _uid = new Uid();

        public event Action Invalidated;

        internal State(IEnumerable<Model.Repository> repos, IEnumerable<Model.Storage> storages, Core core)
        {
            Logger.Debug("Creating new state {0}", _uid);
            Core = core;
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
