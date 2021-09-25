using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model;
using NLog;

namespace BSU.Core.Sync
{
    internal abstract class SyncWorkUnit
    {
        protected readonly StorageMod Storage;
        protected readonly string Path;
        protected readonly Logger Logger = LogManager.GetCurrentClassLogger();

        protected SyncWorkUnit(StorageMod storage, string path)
        {
            Storage = storage;
            Path = path;
        }

        public abstract Task DoAsync(CancellationToken cancellationToken);
        public abstract FileSyncStats GetStats();
    }

    public record FileSyncStats(FileSyncState State, long DownloadTotal, long UpdateTotal, long DownloadDone, long UpdateDone);

    public enum FileSyncState
    {
        Waiting,
        Updating,
        None
    }
}
