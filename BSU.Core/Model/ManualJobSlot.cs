using System;
using BSU.Core.JobManager;
using BSU.Core.Services;

namespace BSU.Core.Model
{
    internal class ManualJobSlot<T> : IJobSlot where T : class, IJob
    {
        private T _job;

        public void StartJob(T job)
        {
            if (_job != null) throw new InvalidOperationException();
            _job = job;
            job.OnFinished += () =>
            {
                _job = null;
                OnFinished?.Invoke();
            };
            ServiceProvider.JobManager.QueueJob(job);
            OnStarted?.Invoke();
        }

        public T GetJob() => _job;

        public bool IsActive() => _job != null;

        public event Action OnFinished, OnStarted;
    }
}