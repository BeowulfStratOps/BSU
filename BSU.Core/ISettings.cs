using System.Collections.Generic;

namespace BSU.Core
{
    public interface ISettings
    {
        void Store();
        List<RepoEntry> Repositories { get; }
        List<StorageEntry> Storages { get; }
    }
}