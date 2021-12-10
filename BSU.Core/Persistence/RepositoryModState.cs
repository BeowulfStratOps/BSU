using System;
using System.Collections.Generic;

namespace BSU.Core.Persistence
{
    internal interface IPersistedRepositoryModState
    {
        PersistedSelection? Selection { get; set; }
    }

    internal class PersistedRepositoryModState : IPersistedRepositoryModState
    {
        private readonly Dictionary<string, PersistedSelection> _usedMods;
        private readonly Action _store;
        private readonly string _identifier;

        public PersistedRepositoryModState(Dictionary<string, PersistedSelection> usedMods, Action store, string identifier)
        {
            _usedMods = usedMods;
            _store = store;
            _identifier = identifier;
        }

        public PersistedSelection? Selection
        {
            get => _usedMods.GetValueOrDefault(_identifier);
            set
            {
                if (value == null)
                {
                    _usedMods.Remove(_identifier);
                    _store();
                    return;
                }

                if (_usedMods.TryGetValue(_identifier, out var oldValue) && oldValue.Equals(value)) return;
                _usedMods[_identifier] = value;
                _store();
            }
        }
    }
}
