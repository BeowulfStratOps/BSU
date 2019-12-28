using System;
using System.Collections.Generic;
using BSU.Core.Hashes;
using BSU.Core.Sync;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.State
{
    /// <summary>
    /// Represents a locally stored mod in the context of a <see cref="BSU.Core.State.State"/>.
    /// </summary>
    public class StorageMod
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public readonly string Name;

        internal readonly MatchHash MatchHash;
        public readonly VersionHash VersionHash;
        internal readonly Storage Storage;

        internal readonly IStorageMod Mod;

        public readonly UpdateTarget UpdateTarget;
        internal readonly RepoSync ActiveJob;
        private readonly List<ModAction> _relatedActions = new List<ModAction>();

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

        public IReadOnlyList<ModAction> GetRelatedActions() => _relatedActions.AsReadOnly();
        internal void AddRelatedAction(ModAction action) => _relatedActions.Add(action);
    }
}
