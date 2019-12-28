using System;
using BSU.Core.Sync;
using BSU.CoreCommon;

namespace BSU.Core.JobManager
{
    /// <summary>
    /// Job, as managed by a JobManager
    /// </summary>
    internal interface IJob
    {
        Uid GetUid();

        /// <summary>
        /// Determines whether this job is completed/errored/aborted.
        /// </summary>
        /// <returns></returns>
        bool IsDone();
        void SetError(Exception e);

        /// <summary>
        /// Trigger done check. Necessary due to bad work unit tracking.
        /// </summary>
        /// <returns></returns>
        void CheckDone();
        void Abort();
        WorkUnit GetWork();
    }
}
