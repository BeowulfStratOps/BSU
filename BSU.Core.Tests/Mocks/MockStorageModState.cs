using BSU.Core.Persistence;

namespace BSU.Core.Tests.Mocks
{
    internal class MockStorageModState : IStorageModState
    {
        public UpdateTarget UpdateTarget { get; set; }
    }
}