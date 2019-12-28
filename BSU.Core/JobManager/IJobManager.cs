using System.Collections.Generic;

namespace BSU.Core.JobManager
{
    internal interface IJobManager<T> where T : IJob
    {
        /// <summary>
        /// Returns all jobs ever queued.
        /// </summary>
        /// <returns></returns>
        IEnumerable<T> GetAllJobs();

        /// <summary>
        /// Return all jobs currently running or queued.
        /// </summary>
        /// <returns></returns>
        IEnumerable<T> GetActiveJobs();

        /// <summary>
        /// Queue a job. Starts execution immediately
        /// </summary>
        /// <param name="job"></param>
        void QueueJob(T job);

        /// <summary>
        /// Shutdown all threads. Does not wait.
        /// </summary>
        void Shutdown();
    }
}
