using System;
using System.Collections.Generic;

namespace BSU.Core.JobManager
{
    internal interface IJobManager
    {
        /// <summary>
        /// Returns all jobs ever queued.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IJob> GetAllJobs();

        /// <summary>
        /// Return all jobs currently running or queued.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IJob> GetActiveJobs();

        /// <summary>
        /// Queue a job. Starts execution immediately
        /// </summary>
        /// <param name="job"></param>
        void QueueJob(IJob job);

        /// <summary>
        /// Shutdown all threads. Does not wait.
        /// </summary>
        void Shutdown(bool blocking);

        event Action<IJob> JobAdded, JobRemoved;
    }
}
