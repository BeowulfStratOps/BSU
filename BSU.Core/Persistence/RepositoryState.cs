using System;
using System.Collections.Generic;
using BSU.Core.Launch;

namespace BSU.Core.Persistence
{
    internal interface IRepositoryState
    {
        IPersistedRepositoryModState GetMod(string identifier);
        Guid Identifier { get; }
        PresetSettings? GetSettings();
        void SetSettings(PresetSettings value);
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
            return new PersistedRepositoryModState(_entry.UsedMods, _store, identifier);
        }

        public Guid Identifier => _entry.Guid;
        public PresetSettings GetSettings() => _entry.Settings;

        public void SetSettings(PresetSettings value)
        {
            _entry.Settings = value;
            _store();
        }
    }
}
