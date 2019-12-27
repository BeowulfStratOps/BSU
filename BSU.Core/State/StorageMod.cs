﻿using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.Hashes;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.State
{
    public class StorageMod
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public readonly string Name;

        internal readonly MatchHash MatchHash;
        public readonly VersionHash VersionHash;
        internal readonly Storage Storage;

        internal readonly IStorageMod Mod;

        public readonly UpdateTarget UpdateTarget;
        internal readonly UpdateJob ActiveJob;
        public readonly List<ModAction> RelatedActions = new List<ModAction>();

        internal readonly Uid Uid = new Uid();

        internal StorageMod(IStorageMod mod, Storage storage)
        {
            Logger.Debug("Creating state for storage mod {0} -> {1}", mod.GetUid(), Uid);
            Mod = mod;
            Storage = storage;
            Name = mod.GetIdentifier();
            Console.WriteLine($"Hashing {storage.Name} / {Name}");
            MatchHash = new MatchHash(mod);
            UpdateTarget = storage.State.Core.GetUpdateTarget(this);

            ActiveJob = storage.State.Core.GetActiveJob(mod);
            if (ActiveJob != null)
            {
                Logger.Debug("Active job is {0}", ActiveJob);
                return;
            }
            VersionHash = new VersionHash(mod);

            if (!VersionHash.GetHashString().Equals(UpdateTarget?.Hash)) return;
            Logger.Info("Storage Mod {0} has met its update target.", mod.GetUid());
            storage.State.Core.UpdateDone(mod);
            UpdateTarget = null;
        }

        public IReadOnlyList<ModAction> GetRelatedActions() => RelatedActions.AsReadOnly();
        internal void AddRelatedAction(ModAction action) => RelatedActions.Add(action);
    }
}
