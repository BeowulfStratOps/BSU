using System;
using BSU.Core.JobManager;
using NLog;

namespace BSU.Core.Model
{
    internal class JobSlot<T>  where T : class, IJob
    {
        // This whole JobSlot thing should be replaces with Tasks and Continuations and cool stuff
        
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
            // TODO: It should never not be null. that would indicate an issue with the statemachine
            if (_job == null) RestartJob();
        }

        public void RestartJob()
        {
            //TODO: use locks
            _job?.Abort();
            Logger.Debug($"Creating Simple Job {_title}");
            _job = _starter();
            _job.OnFinished += () =>
            {
                var error = _job.GetError();
                _job = null;
                Logger.Debug($"Ended Job {_title}");
                OnFinished?.Invoke(error);
            };
            Logger.Debug($"Queueing Job {_title}");
            _jobManager.QueueJob(_job);
        }

        public T GetJob() => _job;

        public event Action<Exception> OnFinished;
    }
}
