using System.Collections.Generic;

namespace BSU.Core.JobManager
{
    internal interface IJobManager<T> where T : IJob
    {
        IEnumerable<T> GetAllJobs();
        IEnumerable<T> GetActiveJobs();
        void QueueJob(T job);
        void Shutdown();
    }
}