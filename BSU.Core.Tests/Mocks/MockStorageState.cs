using System;
using System.Collections.Generic;
using BSU.Core.Persistence;

namespace BSU.Core.Tests.Mocks
{
    internal class MockStorageState : IStorageState
    {
        private readonly Dictionary<string, IPersistedStorageModState> _states = new();

        public IPersistedStorageModState GetMod(string identifier)
        {
            if (_states.TryGetValue(identifier, out var state)) return state;
            state = new PersistedStorageModState(new Dictionary<string, UpdateTarget>(), () => { }, identifier);
            _states[identifier] = state;
            return state;
        }

        public Guid Identifier { get; }
    }
}
