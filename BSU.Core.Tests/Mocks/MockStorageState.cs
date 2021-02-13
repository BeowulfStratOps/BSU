using System;
using BSU.Core.Persistence;

namespace BSU.Core.Tests.Mocks
{
    internal class MockStorageState : IStorageState
    {
        private readonly MockPersistedStorageModState _modState = new MockPersistedStorageModState();
        public IPersistedStorageModState GetMod(string identifier) => _modState;
        public Guid Identifier { get; }
    }
}