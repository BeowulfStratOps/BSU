using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.JobManager;

namespace BSU.Core.Tests
{
    internal class MockJobManager : IJobManager
    {
        private readonly List<IJob> _jobs = new List<IJob>();

        public void QueueJob(IJob job)
        {
            _jobs.Add(job);
        }

        public void Shutdown(bool blocking)
        {
        }

        public event Action<IJob> JobAdded;
        public event Action<IJob> JobRemoved;

        public void DoWork()
        {
            while (_jobs.Any())
            {
                var job = _jobs[0];
                _jobs.Remove(job);
                while (true)
                {
                    if (!job.DoWork()) break;
                }
            }
        }

        public bool DoStep()
        {
            if (!_jobs.Any()) return false;
            var job = _jobs[0];
            if (job.DoWork()) return true;
            _jobs.Remove(job);
            return _jobs.Any();
        }

        public IReadOnlyList<IJob> GetActiveJobs() => _jobs.AsReadOnly();
        public IReadOnlyList<IJob> GetAllJobs() => _jobs.AsReadOnly();
    }
}
