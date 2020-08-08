using BSU.Core.Persistence;

namespace BSU.Core.Tests.Mocks
{
    internal class MockStorageState : IStorageState
    {
        private readonly MockStorageModState _modState = new MockStorageModState();
        public IStorageModState GetMod(string identifier) => _modState;
    }
}