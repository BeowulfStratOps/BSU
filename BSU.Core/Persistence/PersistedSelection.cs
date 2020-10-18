using System;
using BSU.Core.Model;

namespace BSU.Core.Persistence
{
    internal class PersistedSelection : IEquatable<PersistedSelection>
    {
        // TODO: might need a way to store download identifier?
        // TODO: make more explicit regarding DoNothing/Mod/Download

        public Guid? Storage { get; }
        public string Mod { get; }

        public PersistedSelection(Guid? storage, string mod)
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

        public bool Equals(PersistedSelection other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Nullable.Equals(Storage, other.Storage) && Mod == other.Mod;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(PersistedSelection)) return false;
            return Equals((PersistedSelection) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Storage, Mod);
        }

        public static bool operator ==(PersistedSelection left, PersistedSelection right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PersistedSelection left, PersistedSelection right)
        {
            return !Equals(left, right);
        }

        public override string ToString() => $"{Storage?.ToString() ?? "-"}/{Mod??"-"}";
    }
}
