﻿using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.Hashes;
using BSU.CoreCommon;

namespace BSU.Core.State
{
    public class StorageMod
    {
        public readonly string Name;

        internal readonly MatchHash MatchHash;
        public readonly VersionHash VersionHash;
        internal readonly Storage Storage;

        internal readonly ILocalMod Mod;

        public readonly UpdateTarget UpdateTarget;
        internal readonly UpdateJob ActiveJob;
        public readonly List<ModAction> RelatedActions = new List<ModAction>();

        internal StorageMod(ILocalMod mod, Storage storage)
        {
            Mod = mod;
            Storage = storage;
            Name = mod.GetIdentifier();
            Console.WriteLine($"Hashing {storage.Name} / {Name}");
            MatchHash = new MatchHash(mod);
            VersionHash = new VersionHash(mod);
            UpdateTarget = storage.State.Core.GetUpdateTarget(this);

            if (VersionHash.GetHashString().Equals(UpdateTarget?.Hash))
            {
                storage.State.Core.UpdateDone(mod);
                UpdateTarget = null;
            }

            ActiveJob = storage.State.Core.GetActiveJob(mod);
        }

        public IReadOnlyList<ModAction> GetRelatedActions() => RelatedActions.AsReadOnly();
        internal void AddRelatedAction(ModAction action) => RelatedActions.Add(action);
    }
}
