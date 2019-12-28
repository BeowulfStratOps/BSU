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

        internal readonly IRepository BackingRepository;

        internal readonly Uid Uid = new Uid();

        /// <summary>
        /// Remove this repository. Invalidates the state.
        /// </summary>
        public void Remove()
        {
            State.Core.RemoveRepo(this);
            State.InvalidateState();
        }

        internal Repository(IRepository repo, State state)
        {
            BackingRepository = repo;
            Logger.Debug("Creating new repo state for {0} -> {1}", repo.GetUid(), Uid);
            State = state;
            Mods = repo.GetMods().Select(m => new RepositoryMod(m, this)).ToList().AsReadOnly();
            Name = repo.GetIdentifier();
        }

        internal void CollectConflicts()
        {
            foreach (var mod in Mods)
            {
                mod.CollectConflicts();
            }
        }

        /// <summary>
        /// Prepare an update from the currently selected mod actions. Should be used with a using block.
        /// </summary>
        /// <returns></returns>
        public UpdatePacket PrepareUpdate()
        {
            return State.Core.PrepareUpdate(this, State);
        }
    }
}
