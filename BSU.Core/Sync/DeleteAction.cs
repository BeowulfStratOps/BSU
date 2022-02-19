using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;

namespace BSU.Core.Sync
{
    /// <summary>
    /// WorkUnit: Deletes a single file.
    /// </summary>
    internal class DeleteAction : SyncWorkUnit
    {
        public DeleteAction(IStorageMod storage, string path) : base(storage, path)
        {

        }

        public override async Task DoAsync(CancellationToken cancellationToken)
        {
            Logger.Trace($"{Storage} Deleting {Path}");
            await Storage.Delete(Path, cancellationToken);
        }

        public override FileSyncStats GetStats()
        {
            return new FileSyncStats(FileSyncState.Updating);
        }
    }
}
