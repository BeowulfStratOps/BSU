using System;

namespace BSU.Core.View
{
    public class StorageTarget
    {
        internal Model.Actions.StorageTarget ModelStorageTarget;
        
        public StorageMod StorageMod { get; }
        public Storage Storage { get; }
        
        public StorageTarget(StorageMod mod)
        {
            StorageMod = mod;
            ModelStorageTarget = mod.ModelStorageMod.AsTarget;
        }

        public StorageTarget(Storage storage)
        {
            Storage = storage;
            ModelStorageTarget = storage.ModelStorage.AsTarget;
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