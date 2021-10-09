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

    public record FileSyncStats(FileSyncState State, ulong DownloadTotal = 0, ulong UpdateTotal = 0,
        ulong DownloadDone = 0, ulong UpdateDone = 0);

    public enum FileSyncState
    {
        Waiting,
        Updating,
        None,
        Stopping
    }
}
