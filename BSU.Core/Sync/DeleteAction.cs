using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model;
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
            Logger.Trace("{0} Deleting {1}", Storage, Path);
            await Storage.DeleteFile(Path, cancellationToken);
        }

        public override FileSyncStats GetStats()
        {
            return new FileSyncStats(FileSyncState.Updating);
        }
    }
}
