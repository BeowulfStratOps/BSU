using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Sync
{
    internal abstract class SyncWorkUnit
    {
        protected readonly IStorageMod Storage;
        protected readonly string Path;
        protected readonly Logger Logger = LogManager.GetCurrentClassLogger();

        protected SyncWorkUnit(IStorageMod storage, string path)
        {
            Storage = storage;
            Path = path;
        }

        public abstract Task DoAsync(CancellationToken cancellationToken);
        public abstract FileSyncStats GetStats();
    }

    public record FileSyncStats(FileSyncState State, ulong Total = 0, ulong Done = 0);

    public enum FileSyncState
    {
        Waiting,
        Updating,
        None,
        Stopping
    }
}
