using System;
using System.Collections.Generic;
using BSU.Core.JobManager;
using BSU.Core.Services;
using NLog;

namespace BSU.Core.Model
{
    internal interface IJobSlot
    {
        event Action OnStarted, OnFinished;
        bool IsActive();
    }
    
    internal class JobSlot<T> : IJobSlot where T : class, IJob
    {
        private static readonly Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        
        private readonly Func<T> _starter;
        private readonly string _title;
        private T _job;

        internal JobSlot(Func<T> starter, string title)
        {
            _starter = starter;
            _title = title;
        }
        
        public bool IsActive() => _job != null;

        public void StartJob()
        {
            if (_job == null) RestartJob();
        }

        public void RestartJob()
        {
            _job?.Abort();
            Logger.Debug($"Creating Simple Job {_title}");
            _job = _starter();
            _job.OnFinished += () =>
            {
                _job = null;
                Logger.Debug($"Ended Job {_title}");
                OnFinished?.Invoke();
            };
            Logger.Debug($"Queueing Job {_title}");
            ServiceProvider.JobManager.QueueJob(_job);
            OnStarted?.Invoke();
        }

        public T GetJob() => _job;

        public event Action OnStarted, OnFinished;
    }
}
