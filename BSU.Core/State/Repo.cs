using System;
using System.Collections.Generic;
using System.Linq;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.State
{
    public class Repo
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public readonly List<RepositoryMod> Mods;
        public readonly string Name;

        internal readonly State State;

        internal readonly Uid Uid = new Uid();

        internal Repo(IRepository repo, State state)
        {
            Logger.Debug("Creating new repo state for {0} -> {1}", repo.GetUid(), Uid);
            State = state;
            Mods = repo.GetMods().Select(m => new RepositoryMod(m, this)).ToList();
            Name = repo.GetName();
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
