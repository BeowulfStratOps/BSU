using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.CoreCommon;
using NLog;

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

       public UpdateAction(IRepositoryMod repository, StorageMod storage, string path, ulong fileSize)
            : base(storage, path)
       {
           _repository = repository;
           _fileSize = fileSize;
       }

        public override async Task DoAsync(CancellationToken cancellationToken)
        {
            Logger.Trace("{0}, {1} Updating {2}", Storage, _repository, Path);
            await using var target = await Storage.Implementation.OpenFile(Path.ToLowerInvariant(), FileAccess.ReadWrite, cancellationToken);
            var progress = new Progress<ulong>();
            progress.ProgressChanged += (_, count) => _done += count;
            await _repository.UpdateTo(Path, target, progress, cancellationToken);
        }

        public override FileSyncStats GetStats()
        {
            return new FileSyncStats(FileSyncState.Updating, 0, _fileSize, 0, _done);
        }
    }
}
