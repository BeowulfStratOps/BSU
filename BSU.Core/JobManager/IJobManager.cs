using System.Collections.Generic;

namespace BSU.Core.JobManager
{
    internal interface IJobManager<T> where T : IJob
    {
        IReadOnlyList<T> GetAllJobs();
        IReadOnlyList<T> GetActiveJobs();
        void QueueJob(T job);
        void Shutdown();
    }
}
