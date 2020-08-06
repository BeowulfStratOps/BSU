using System.Collections.Generic;

namespace BSU.Core.Tests
{
    internal class MockSettings : ISettings
    {
        public List<RepositoryEntry> Repositories { get; set; } = new List<RepositoryEntry>();

        public List<StorageEntry> Storages { get; set; } = new List<StorageEntry>();

        public void Store()
        {
        }
    }
}
