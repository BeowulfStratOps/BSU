using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.JobManager;
using BSU.Core.Model;

namespace BSU.Core.Tests.Mocks
{
    internal class MockWorker : IJobManager, IActionQueue
    {
        private readonly List<IJob> _jobs = new List<IJob>();
        private readonly Queue<Action> _actionQueue = new Queue<Action>();

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
            while (DoJobStep() || DoQueueStep())
            {
            }
        }

        public bool DoJobStep()
        {
            if (!_jobs.Any()) return false;
            var job = _jobs[0];
            if (job.DoWork()) return true;
            _jobs.Remove(job);
            return true;
        }
        
        public bool DoQueueStep()
        {
            if (!_actionQueue.Any()) return false;
            _actionQueue.Dequeue()();
            return true;
        }

        public IReadOnlyList<IJob> GetActiveJobs() => _jobs.AsReadOnly();
        public IReadOnlyList<IJob> GetAllJobs() => _jobs.AsReadOnly();
        public void EnQueueAction(Action action)
        {
            _actionQueue.Enqueue(action);
        }
    }
}