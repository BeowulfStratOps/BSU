using System.Collections.Generic;
using System.Linq;
using BSU.Core.JobManager;
using BSU.Core.Sync;

namespace BSU.Core.Tests
{
    internal class MockJobManager : IJobManager
    {
        private readonly List<RepoSync> _jobs = new List<RepoSync>();

        public void QueueJob(IJob job)
        {
            _jobs.Add(job as RepoSync);
        }

        public void Shutdown(bool blocking)
        {
        }

        public void DoWork()
        {
            while (_jobs.Any())
            {
                var job = _jobs[0];
                var work = job.GetWork();
                while (work != null)
                {
                   work.Work();
                   work = job.GetWork();
                }
            }
        }

        public IEnumerable<IJob> GetActiveJobs() => _jobs.AsReadOnly();
        public IEnumerable<IJob> GetAllJobs() => _jobs.AsReadOnly();
    }
}
