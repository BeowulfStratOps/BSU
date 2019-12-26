using System;

namespace BSU.Core.State
{
    public class JobView
    {
        internal readonly UpdateJob Job;
        public readonly string TargetHash;

        public delegate void JobEndedDelegate(bool success);
        public event JobEndedDelegate JobEnded;

        internal JobView(UpdateJob job)
        {
            Job = job;
            job.SyncState.SyncEnded += success => JobEnded?.Invoke(success);
            TargetHash = job.Target.Hash;
        }

        public string GetStorageModDisplayName() => Job.StorageMod.GetDisplayName();
        public string GetRepositoryModDisplayName() => Job.RepositoryMod.GetDisplayName();

        public int GetRemainingNewFilesCount() => Job.SyncState.GetRemainingNewFilesCount();
        public int GetRemainingChangedFilesCount() => Job.SyncState.GetRemainingChangedFilesCount();
        public int GetRemainingDeletedFilesCount() => Job.SyncState.GetRemainingDeletedFilesCount();
        public long GetRemainingBytesToDownload() => Job.SyncState.GetRemainingBytesToDownload();
        public long GetRemainingBytesToUpdate() => Job.SyncState.GetRemainingBytesToUpdate();

        public int GetTotalNewFilesCount() => Job.SyncState.GetTotalNewFilesCount();
        public int GetTotalChangedFilesCount() => Job.SyncState.GetTotalChangedFilesCount();
        public int GetTotalDeletedFilesCount() => Job.SyncState.GetTotalDeletedFilesCount();
        public long GetTotalBytesToDownload() => Job.SyncState.GetTotalBytesToDownload();
        public long GetTotalBytesToUpdate() => Job.SyncState.GetTotalBytesToUpdate();

        public bool IsDone() => Job.SyncState.IsDone();
        public Exception GetError() => Job.SyncState.GetError();

        public void Abort() => Job.SyncState.Abort();
    }
}
