using BSU.Core.Persistence;

namespace BSU.Core.Tests.Test
{
    internal class MockStorageModState : IStorageModState
    {
        public UpdateTarget UpdateTarget { get; set; }
    }
}