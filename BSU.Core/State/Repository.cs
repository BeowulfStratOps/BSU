using System.Collections.Generic;
using System.Linq;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.State
{
    /// <summary>
    /// Represents a repository as part of a <see cref="BSU.Core.State.State"/>.
    /// </summary>
    public class Repository
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public readonly IReadOnlyList<RepositoryMod> Mods;
        public readonly string Name;

        internal readonly State State;

        internal readonly Model.Repository BackingRepository;

        internal readonly Uid Uid = new Uid();

        /// <summary>
        /// Remove this repository. Invalidates the state.
        /// </summary>
        public void Remove()
        {
            State.InvalidateState();
        }

        internal Repository(Model.Repository repo, State state)
        {
            BackingRepository = repo;
            Logger.Debug("Creating new repo state for {0} -> {1}", repo.Uid, Uid);
            State = state;
            Mods = repo.Mods.Select(m => new RepositoryMod(m, this)).ToList().AsReadOnly();
            Name = repo.Identifier;
        }

        internal void CollectConflicts()
        {
            foreach (var mod in Mods)
            {
                mod.CollectConflicts();
            }
        }
    }
}
