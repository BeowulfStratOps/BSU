using System;
using BSU.Core.Model;

namespace BSU.Core.Persistence
{
    internal class PersistedSelection
    {
        // TODO: might need a way to store download identifier?
        // TODO: make more explicit regarding DoNothing/Mod/Download

        public string Storage { get; }
        public string Mod { get; }

        public PersistedSelection(string storage, string mod)
        {
            Storage = storage;
            Mod = mod;
        }

        public static PersistedSelection Create(RepositoryModActionSelection storage)
        {
            if (storage.DoNothing) return new PersistedSelection(null, null);
            if (storage.StorageMod != null) return storage.StorageMod.GetStorageModIdentifiers();
            if (storage.DownloadStorage != null) return storage.DownloadStorage.GetStorageIdentifier();
            throw new ArgumentException();
        }

        protected bool Equals(PersistedSelection other)
        {
            return Storage == other.Storage && Mod == other.Mod;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Storage, Mod);
        }
    }
}
