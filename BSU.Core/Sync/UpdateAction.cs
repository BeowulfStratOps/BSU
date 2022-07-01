using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;

namespace BSU.Core.Sync
{
    /// <summary>
    /// WorkUnit: Updates a single file from a remote location. Refers to the RepositoryMod for the actual operation.
    /// </summary>
    internal class UpdateAction : SyncWorkUnit
    {
       private readonly IRepositoryMod _repository;
       private readonly ulong _fileSize;
       private ulong _done;

       public UpdateAction(IRepositoryMod repository, IStorageMod storage, string path, ulong fileSize)
            : base(storage, path)
       {
           _repository = repository;
           _fileSize = fileSize;
       }

        public override async Task DoAsync(CancellationToken cancellationToken)
        {
            Logger.Trace($"{Storage}, {_repository} Updating {Path}");
            var progress = new SynchronousProgress<ulong>();
            progress.ProgressChanged += (_, done) => _done = done;
            // updating might require some CPU resources, so we want it on the threadpool
            await Task.Run(() => _repository.UpdateTo(Path, Storage, progress, cancellationToken), cancellationToken);
        }

        public override FileSyncStats GetStats()
        {
            return new FileSyncStats(FileSyncState.Updating, _fileSize, _done);
        }
    }
}
