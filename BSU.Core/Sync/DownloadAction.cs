using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;

namespace BSU.Core.Sync
{
    /// <summary>
    /// WorkUnit: Downloads a new file. Refers to the RepositoryMod for the actual download.
    /// </summary>
    internal class DownloadAction : SyncWorkUnit
    {
        private readonly IRepositoryMod _repository;
        private readonly ulong _fileSize;
        private ulong _done;

        public DownloadAction(IRepositoryMod repository, IStorageMod storage, string path, ulong fileSize) : base(storage, path)
        {
            _repository = repository;
            _fileSize = fileSize;
        }

        public override async Task DoAsync(CancellationToken cancellationToken)
        {
            Logger.Trace($"{_repository} Downloading {Path}");
            var progress = new SynchronousProgress<ulong>();
            progress.ProgressChanged += (_, done) => _done = done;
            await Task.Run(() => _repository.DownloadTo(Path, Storage, progress, cancellationToken), cancellationToken);
        }

        public override FileSyncStats GetStats()
        {
            return new FileSyncStats(FileSyncState.Updating, _fileSize, _done);
        }
    }
}
