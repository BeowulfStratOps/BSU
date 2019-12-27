using System;

namespace BSU.Core
{
    public interface IJobFacade
    {
        bool HasError();
        bool IsDone();
        string GetTargetHash();

        public delegate void JobEndedDelegate(bool success);

        event JobEndedDelegate JobEnded;

        public string GetStorageModDisplayName();
        public string GetRepositoryModDisplayName();

        public int GetRemainingNewFilesCount();
        public int GetRemainingChangedFilesCount();
        public int GetRemainingDeletedFilesCount();
        public long GetRemainingBytesToDownload();
        public long GetRemainingBytesToUpdate();

        public int GetTotalNewFilesCount();
        public int GetTotalChangedFilesCount();
        public int GetTotalDeletedFilesCount();
        public long GetTotalBytesToDownload();
        public long GetTotalBytesToUpdate();

        Exception GetError();

        void Abort();
    }
}