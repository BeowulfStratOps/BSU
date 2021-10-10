using System.Collections.Generic;
using BSU.Core.Persistence;

namespace BSU.Core.Tests.Mocks
{
    internal class MockSettings : ISettings
    {
        public void Store()
        {
        }

        public List<RepositoryEntry> Repositories { get; } = new List<RepositoryEntry>();
        public List<StorageEntry> Storages { get; } = new List<StorageEntry>();
        public bool FirstStartDone { get; set; }
    }
}
