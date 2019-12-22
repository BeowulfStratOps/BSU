using System;

namespace BSU.Core.State
{
    public class JobView
    {
        private readonly UpdateJob _job;
        public readonly string TargetHash;

        public delegate void JobEndedDelegate(bool success);
        public event JobEndedDelegate JobEnded;

        internal JobView(UpdateJob job)
        {
            _job = job;
            job.SyncState.SyncEnded += success => JobEnded?.Invoke(success);
            TargetHash = job.Target.Hash;
        }

        public string GetStorageModDisplayName() => _job.StorageMod.GetDisplayName();
        public string GetRepositoryModDisplayName() => _job.RepositoryMod.GetDisplayName();

        public int GetRemainingNewFilesCount() => _job.SyncState.GetRemainingNewFilesCount();
        public int GetRemainingChangedFilesCount() => _job.SyncState.GetRemainingChangedFilesCount();
        public int GetRemainingDeletedFilesCount() => _job.SyncState.GetRemainingDeletedFilesCount();
        public long GetRemainingBytesToDownload() => _job.SyncState.GetRemainingBytesToDownload();
        public long GetRemainingBytesToUpdate() => _job.SyncState.GetRemainingBytesToUpdate();

        public int GetTotalNewFilesCount() => _job.SyncState.GetTotalNewFilesCount();
        public int GetTotalChangedFilesCount() => _job.SyncState.GetTotalChangedFilesCount();
        public int GetTotalDeletedFilesCount() => _job.SyncState.GetTotalDeletedFilesCount();
        public long GetTotalBytesToDownload() => _job.SyncState.GetTotalBytesToDownload();
        public long GetTotalBytesToUpdate() => _job.SyncState.GetTotalBytesToUpdate();

        public bool IsDone() => _job.SyncState.IsDone();
        public Exception GetError() => _job.SyncState.GetError();

        public void Abort() => _job.SyncState.Abort();
        public bool Aborted => _job.SyncState.Aborted;
    }
}
