using System;
using BSU.Core.Hashes;
using BSU.CoreInterface;

namespace BSU.Core.State
{
    public class StorageMod
    {
        public readonly string Name, Location;

        internal readonly MatchHash _matchHash;
        public readonly VersionHash VersionHash;
        internal readonly Storage Storage;

        private readonly ILocalMod _mod;

        internal StorageMod(ILocalMod mod, Storage storage)
        {
            _mod = mod;
            Storage = storage;
            Name = mod.GetIdentifier();
            Console.WriteLine($"Hashing {storage.Name} / {Name}");
            _matchHash = new MatchHash(mod);
            VersionHash = new VersionHash(mod);
            Location = mod.GetBaseDirectory().FullName;
        }

        /*public string Name, DisplayName, Location;
        public StorageView Parent;
        public List<RepoModView> UsedBy;

        // can't be broken and updating.
        public bool IsBroken;
        public RepoModView UpdatingTo;*/
    }
}
