using BSU.Core.Persistence;

namespace BSU.Core.Tests.Mocks
{
    internal class MockRepositoryModState : IRepositoryModState
    {
        public PersistedSelection Selection { get; set; }
    }
}