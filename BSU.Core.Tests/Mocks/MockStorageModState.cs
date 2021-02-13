using BSU.Core.Persistence;

namespace BSU.Core.Tests.Mocks
{
    internal class MockPersistedStorageModState : IPersistedStorageModState
    {
        public UpdateTarget UpdateTarget { get; set; }
    }
}