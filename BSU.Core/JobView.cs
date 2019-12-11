namespace BSU.Core
{
    public class JobView
    {
        private UpdateJob _job;
        public readonly string TargetHash;

        internal JobView(UpdateJob job)
        {
            _job = job;
            TargetHash = job.Target.Hash;
        }

        public string GetLocalDisplayName() => _job.LocalMod.GetDisplayName();
        public string GetRemoteDisplayName() => _job.RemoteMod.GetDisplayName();
        
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
    }
}