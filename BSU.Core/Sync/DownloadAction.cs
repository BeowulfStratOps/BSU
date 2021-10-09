using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model;
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

        public DownloadAction(IRepositoryMod repository, StorageMod storage, string path, ulong fileSize) : base(storage, path)
        {
            _repository = repository;
            _fileSize = fileSize;
        }

        public override async Task DoAsync(CancellationToken cancellationToken)
        {
            Logger.Trace("{0}, {1} Downloading {2}", _repository, _repository, Path);
            await using var target = await Storage.Implementation.OpenFile(Path.ToLowerInvariant(), FileAccess.Write, cancellationToken);
            var progress = new Progress<ulong>();
            progress.ProgressChanged += (_, count) => _done += count;
            await _repository.DownloadTo(Path, target, progress, cancellationToken);
        }

        public override FileSyncStats GetStats()
        {
            return new FileSyncStats(FileSyncState.Updating, _fileSize, 0, _done, 0);
        }
    }
}
