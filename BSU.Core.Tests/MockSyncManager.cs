using System.Collections.Generic;

namespace BSU.Core.Tests
{
    internal class MockSyncManager : ISyncManager
    {
        private readonly List<UpdateJob> _jobs = new List<UpdateJob>();

        public void QueueJob(UpdateJob job)
        {
            _jobs.Add(job);
        }

        public void Shutdown()
        {
        }

        public IReadOnlyList<UpdateJob> GetActiveJobs() => _jobs.AsReadOnly();
        public IReadOnlyList<UpdateJob> GetAllJobs() => _jobs.AsReadOnly();
    }
}
