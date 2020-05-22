using System;
using BSU.Core.JobManager;
using NLog;

namespace BSU.Core.Model
{
    internal class JobSlot<T>  where T : class, IJob
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        private readonly Func<T> _starter;
        private readonly string _title;
        private readonly IJobManager _jobManager;
        private T _job;

        internal JobSlot(Func<T> starter, string title, IJobManager jobManager)
        {
            _starter = starter;
            _title = title;
            _jobManager = jobManager;
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
            _jobManager.QueueJob(_job);
        }

        public T GetJob() => _job;

        public event Action OnFinished;
    }
}
