using System;

namespace BSU.Core.Model.Actions
{
    internal class StorageTarget
    {
        public StorageMod StorageMod { get; }
        public Storage Storage { get; }
        
        public StorageTarget(StorageMod mod)
        {
            StorageMod = mod;
        }

        public StorageTarget(Storage storage)
        {
            Storage = storage;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is StorageTarget other)) return false;
            return StorageMod == other.StorageMod && Storage == other.Storage;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StorageMod, Storage);
        }
    }
}