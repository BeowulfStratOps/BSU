using System.Collections.Generic;
using BSU.Core.JobManager;
using BSU.Core.Sync;

namespace BSU.Core.Tests
{
    internal class MockSyncManager : IJobManager<RepoSync>
    {
        private readonly List<RepoSync> _jobs = new List<RepoSync>();

        public void QueueJob(RepoSync job)
        {
            _jobs.Add(job);
        }

        public void Shutdown()
        {
        }

        public IEnumerable<RepoSync> GetActiveJobs() => _jobs.AsReadOnly();
        public IEnumerable<RepoSync> GetAllJobs() => _jobs.AsReadOnly();
    }
}
