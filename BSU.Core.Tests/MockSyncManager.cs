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

        public IReadOnlyList<RepoSync> GetActiveJobs() => _jobs.AsReadOnly();
        public IReadOnlyList<RepoSync> GetAllJobs() => _jobs.AsReadOnly();
    }
}
