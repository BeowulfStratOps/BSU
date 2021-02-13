using BSU.Core.Persistence;

namespace BSU.Core.Tests.Mocks
{
    internal class MockPersistedRepositoryModState : IPersistedRepositoryModState
    {
        public PersistedSelection Selection { get; set; }
    }
}