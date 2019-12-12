using System;
using System.Linq;
using BSU.Core.Hashes;
using BSU.CoreInterface;

namespace BSU.Core.State
{
    public class StorageMod
    {
        public readonly string Name;

        internal readonly MatchHash _matchHash;
        public readonly VersionHash VersionHash;
        internal readonly Storage Storage;

        internal readonly ILocalMod Mod;

        public readonly UpdateTarget UpdateTarget;
        internal readonly UpdateJob ActiveJob;

        internal StorageMod(ILocalMod mod, Storage storage)
        {
            Mod = mod;
            Storage = storage;
            Name = mod.GetIdentifier();
            Console.WriteLine($"Hashing {storage.Name} / {Name}");
            _matchHash = new MatchHash(mod);
            VersionHash = new VersionHash(mod);
            UpdateTarget = storage.State._core.GetUpdateTarget(this);
            ActiveJob = storage.State._core.GetActiveJob(mod);
        }

        /*public string Name, DisplayName, Location;
        public StorageView Parent;
        public List<RepoModView> UsedBy;

        // can't be broken and updating.
        public bool IsBroken;
        public RepoModView UpdatingTo;*/
    }
}
