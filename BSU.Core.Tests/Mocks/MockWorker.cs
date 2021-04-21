using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.JobManager;
using BSU.Core.Model;
using NLog;

namespace BSU.Core.Tests.Mocks
{
    internal class MockWorker : IJobManager, IActionQueue
    {
        private readonly List<IJob> _jobs = new List<IJob>();
        private readonly Queue<Action> _actionQueue = new Queue<Action>();
        private readonly Logger _logger = EntityLogger.GetLogger();
        private readonly bool _processImmediately;

        public MockWorker(bool processImmediately = false)
        {
            _processImmediately = processImmediately;
        }

        public void QueueJob(IJob job)
        {
            _logger.Debug("Queueing job {0}: {1}", job.GetUid(), job.GetTitle());
            _jobs.Add(job);
            if (_processImmediately) DoWork();
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
            if (job.DoWork(this)) return true;
            _jobs.Remove(job);
            _logger.Debug("Removed job {0}: {1}", job.GetUid(), job.GetTitle());
            return true;
        }
        
        private bool DoQueueStep()
        {
            lock (_actionQueue)
            {
                if (!_actionQueue.Any()) return false;
                _actionQueue.Dequeue()();
                return true;
            }
        }

        public IReadOnlyList<IJob> GetActiveJobs() => _jobs.AsReadOnly();
        public IReadOnlyList<IJob> GetAllJobs() => _jobs.AsReadOnly();
        public void EnQueueAction(Action action)
        {
            lock (_actionQueue)
            {
                _actionQueue.Enqueue(action);
            }
        }
    }
}
