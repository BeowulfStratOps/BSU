using System;
using System.Collections.Generic;
using BSU.Core.JobManager;
using BSU.Core.Services;

namespace BSU.Core.Model
{
    internal interface IJobSlot
    {
        event Action OnStarted, OnFinished;
        bool IsActive();
    }
    
    internal class JobSlot<T> : IJobSlot where T : class, IJob
    {
        private readonly Func<T> _starter;
        private T _job;

        internal JobSlot(Func<T> starter)
        {
            _starter = starter;
        }
        
        public bool IsActive() => _job != null;

        public void StartJob()
        {
            if (_job == null) RestartJob();
        }

        public void RestartJob()
        {
            _job?.Abort();
            _job = _starter();
            _job.OnFinished += () => { _job = null; OnFinished?.Invoke(); };
            ServiceProvider.JobManager.QueueJob(_job);
            OnStarted?.Invoke();
        }

        public T GetJob() => _job;

        public event Action OnStarted, OnFinished;
    }
}
