using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model;

namespace BSU.Core.Sync
{
    /// <summary>
    /// WorkUnit: Deletes a single file.
    /// </summary>
    internal class DeleteAction : SyncWorkUnit
    {
        public DeleteAction(StorageMod storage, string path) : base(storage, path)
        {

        }

        public override async Task DoAsync(CancellationToken cancellationToken)
        {
            Logger.Trace("{0} Deleting {1}", Storage, Path);
            await Storage.Implementation.DeleteFile(Path, cancellationToken);
        }
    }
}
