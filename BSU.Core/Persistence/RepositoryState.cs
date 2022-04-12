using System;
using System.Collections.Generic;

namespace BSU.Core.Persistence
{
    internal interface IRepositoryState
    {
        IPersistedRepositoryModState GetMod(string identifier);
        Guid Identifier { get; }
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

        public IPersistedRepositoryModState GetMod(string identifier)
        {
            PersistedSelection? Get()
            {
                return _entry.UsedMods.GetValueOrDefault(identifier);
            }

            void Set(PersistedSelection? value)
            {
                if (value == null)
                {
                    _entry.UsedMods.Remove(identifier);
                    _store();
                    return;
                }

                if (_entry.UsedMods.TryGetValue(identifier, out var oldValue) && oldValue.Equals(value)) return;
                _entry.UsedMods[identifier] = value;
                _store();
            }

            return new PersistedRepositoryModState(Get, Set);
        }

        public Guid Identifier => _entry.Guid;
    }
}
