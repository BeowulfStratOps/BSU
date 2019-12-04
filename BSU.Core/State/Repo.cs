﻿using System;
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

        public UpdatePacket PrepareUpdate()
        {
            if (Mods.Any(m => m.Selected == null))
                throw new InvalidOperationException("Some mods don't have an action selected.");

            return State._core.PrepareUpdate(this);
        }
    }
}