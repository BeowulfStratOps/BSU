using System;

namespace BSU.Core.Persistence
{
    internal interface IRepositoryState
    {
        IRepositoryModState GetMod(string identifier);
    }
    
    internal class RepositoryState : IRepositoryState
    {
        private readonly RepositoryEntry _entry;
        private readonly Action _store;

        public RepositoryState(RepositoryEntry entry, Action store)
        {
            _entry = entry;
            _store = store;
        }
        
        public IRepositoryModState GetMod(string identifier)
        {
            return new RepositoryModState(_entry.UsedMods, _store, identifier);
        }
    }
}