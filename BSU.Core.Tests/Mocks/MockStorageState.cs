using System;
using BSU.Core.Persistence;

namespace BSU.Core.Tests.Mocks
{
    internal class MockStorageState : IStorageState
    {
        public IPersistedStorageModState GetMod(string identifier)
        {
            throw new NotImplementedException();
        }

        public Guid Identifier { get; }
    }
}
