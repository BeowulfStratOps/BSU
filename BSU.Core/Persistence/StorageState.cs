using System;

namespace BSU.Core.Persistence
{
    internal class StorageState : IStorageState
    {
        private readonly StorageEntry _entry;
        private readonly Action _store;

        public StorageState(StorageEntry entry, Action store)
        {
            _entry = entry;
            _store = store;
        }

        public IStorageModState GetMod(string identifier)
        {
            return new StorageModState(_entry.Updating, _store, identifier);
        }
    }
}