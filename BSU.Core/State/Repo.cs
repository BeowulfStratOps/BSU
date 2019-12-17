using System;
using System.Collections.Generic;
using System.Linq;
using BSU.CoreInterface;

namespace BSU.Core.State
{
    public class Repo
    {
        public readonly List<RepoMod> Mods;
        public readonly string Name;

        internal readonly State State;

        internal Repo(IRepository repo, State state)
        {
            State = state;
            Mods = repo.GetMods().Select(m => new RepoMod(m, this)).ToList();
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
            return State.Core.PrepareUpdate(this);
        }
    }
}