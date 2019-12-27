using System.Collections.Generic;

namespace BSU.Core.Tests
{
    internal class MockSettings : ISettings
    {
        public List<RepoEntry> Repositories { get; set; } = new List<RepoEntry>();

        public List<StorageEntry> Storages { get; set; } = new List<StorageEntry>();

        public void Store()
        {
        }
    }
}
