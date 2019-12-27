using System;
using System.Collections.Generic;
using System.Linq;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.State
{
    public class Repository
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public readonly IReadOnlyList<RepositoryMod> Mods;
        public readonly string Name;

        internal readonly State State;

        internal readonly IRepository BackingRepository;

        internal readonly Uid Uid = new Uid();

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

        public UpdatePacket PrepareUpdate()
        {
            return State.Core.PrepareUpdate(this, State);
        }
    }
}
