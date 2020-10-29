using System;
using BSU.Core.JobManager;
using BSU.CoreCommon;
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
        private readonly object _lock = new object();
        private readonly Uid _uid = new Uid();

        internal JobSlot(Func<T> starter, string title, IJobManager jobManager)
        {
            _starter = starter;
            _title = title;
            _jobManager = jobManager;
        }
        
        public bool IsActive() => _job != null;
        
        public void StartJob()
        {
            lock (_lock)
            {
                if (_job != null) throw new InvalidOperationException();
                _job?.Abort();
                Logger.Debug($"Creating Simple Job {_title}");
                _job = _starter();
                Logger.Trace("Starting job {0} from slot {1}", _job.GetUid(), _uid);
                if (_job == null) throw new NullReferenceException("reeee2");
                _job.OnFinished += () =>
                {
                    lock (_lock)
                    {
                        Logger.Debug($"Ended1 Job {_title}");
                        if (_job == null) throw new NullReferenceException("reeee " + _uid);
                        var error = _job.GetError();
                        _job = null;
                        Logger.Debug($"Ended Job {_title}");
                        OnFinished?.Invoke(error);
                    }
                };
                Logger.Debug($"Queueing Job {_title}");
                _jobManager.QueueJob(_job);
            }
        }

        public T GetJob() => _job;

        public event Action<Exception> OnFinished;
    }
}
