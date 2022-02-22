using System.Collections.Generic;
using BSU.Core.Launch;

namespace BSU.Core.Persistence
{
    internal interface ISettings
    {
        void Store();
        List<RepositoryEntry> Repositories { get; }
        List<StorageEntry> Storages { get; }
        bool FirstStartDone { get; set; }
        GlobalSettings GlobalSettings { get; set; }
    }
}
