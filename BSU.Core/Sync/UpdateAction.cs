﻿using System.Threading;
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
            progress.ProgressChanged += (_, count) => _done += count;
            await _repository.UpdateTo(Path, Storage, progress, cancellationToken);
        }

        public override FileSyncStats GetStats()
        {
            return new FileSyncStats(FileSyncState.Updating, _fileSize, _done);
        }
    }
}
