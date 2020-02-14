using System;

namespace BSU.Core
{
    public interface IJobFacade
    {
        /// <summary>
        /// Returns whether an error happened during the execution of this job.
        /// </summary>
        /// <returns></returns>
        bool HasError();

        /// <summary>
        /// Determines whether this job is completed/errored/aborted.
        /// </summary>
        /// <returns></returns>
        bool IsDone();

        /// <summary>
        /// Returns the VersionHash this job is aiming for.
        /// </summary>
        /// <returns></returns>
        string GetTargetHash();

        /// <summary>
        /// Triggered when <see cref="IsDone"/> becomes true.
        /// </summary>
        event Action OnFinished;

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

        /// <summary>
        /// Returns an error that happened during execution, or null.
        /// </summary>
        /// <returns></returns>
        Exception GetError();

        /// <summary>
        /// Signals to abort this job. Does not wait.
        /// </summary>
        void Abort();
    }
}
