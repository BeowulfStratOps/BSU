using System;
using BSU.Core.JobManager;

namespace BSU.Core.Model
{
    internal class ManualJobSlot<T> : IJobSlot where T : class, IJob
    {
        private readonly IJobManager _jobManager;
        private T _job;
        
        public ManualJobSlot(IJobManager jobManager)
        {
            _jobManager = jobManager;
        }

        public void StartJob(T job)
        {
            if (_job != null) throw new InvalidOperationException();
            _job = job;
            job.OnFinished += () =>
            {
                _job = null;
                OnFinished?.Invoke();
            };
            _jobManager.QueueJob(job);
            OnStarted?.Invoke();
        }

        public T GetJob() => _job;

        public bool IsActive() => _job != null;

        public event Action OnFinished, OnStarted;
    }
}