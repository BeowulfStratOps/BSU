using System.Collections.Generic;
using BSU.Core.Launch;
using BSU.Core.Persistence;

namespace BSU.Core.Tests.Mocks
{
    internal class MockSettings : ISettings
    {
        public void Store()
        {
        }

        public List<RepositoryEntry> Repositories { get; } = new();
        public List<StorageEntry> Storages { get; } = new();
        public bool FirstStartDone { get; set; }
        public GlobalSettings GlobalSettings { get; set; } = new();
    }
}
