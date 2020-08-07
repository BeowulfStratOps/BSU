using System;
using System.Collections.Generic;

namespace BSU.Core.Persistence
{
    internal interface IRepositoryModState
    {
        StorageModIdentifiers UsedMod { get; set; }
    }
    
    internal class RepositoryModState : IRepositoryModState
    {
        private readonly Dictionary<string, StorageModIdentifiers> _usedMods;
        private readonly Action _store;
        private readonly string _identifier;

        public RepositoryModState(Dictionary<string, StorageModIdentifiers> usedMods, Action store, string identifier)
        {
            _usedMods = usedMods ?? new Dictionary<string, StorageModIdentifiers>(); // TODO: is this the right place?
            _store = store;
            _identifier = identifier;
        }

        public StorageModIdentifiers UsedMod
        {
            get => _usedMods.GetValueOrDefault(_identifier);
            set
            {
                if (value == null)
                    _usedMods.Remove(_identifier);
                else
                    _usedMods[_identifier] = value;
                _store();
            }
        }
    }
}