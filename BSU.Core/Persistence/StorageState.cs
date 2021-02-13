using System;

namespace BSU.Core.Persistence
{
    internal interface IStorageState
    {
        IPersistedStorageModState GetMod(string identifier);
        Guid Identifier { get; }
    }
    
    internal class StorageState : IStorageState
    {
        private readonly StorageEntry _entry;
        private readonly Action _store;

        public StorageState(StorageEntry entry, Action store)
        {
            _entry = entry;
            _store = store;
        }

        public IPersistedStorageModState GetMod(string identifier)
        {
            return new PersistedStorageModState(_entry.Updating, _store, identifier);
        }

        public Guid Identifier => _entry.Guid;
    }
}