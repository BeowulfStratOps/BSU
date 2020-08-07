using System;

namespace BSU.Core.Persistence
{
    internal class StorageModIdentifiers
    {
        public string Storage { get; }
        public string Mod { get; }

        public StorageModIdentifiers(string storage, string mod)
        {
            Storage = storage;
            Mod = mod;
        }
        
        protected bool Equals(StorageModIdentifiers other)
        {
            return Storage == other.Storage && Mod == other.Mod;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Storage, Mod);
        }
    }
}