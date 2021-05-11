using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.JobManager;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class JobSlot
    {
        private readonly Logger _logger = EntityLogger.GetLogger();

        private readonly Action<CancellationToken> _action;
        private readonly string _title;
        private readonly IJobManager _jobManager;
        private SimpleJob _job;

        internal JobSlot(Action<CancellationToken> action, string title, IJobManager jobManager)
        {
            _action = action;
            _title = title;
            _jobManager = jobManager;
        }

        public event Action Done;

        public void Request()
        {
            if (_job != null) return;
            _logger.Debug($"Creating Simple Job {_title}");
            _job = new SimpleJob(_action, _title, 1);
            _job.Done += () => Done?.Invoke();  // TODO: memory leak
            _jobManager.QueueJob(_job);
        }

        public void Reset()
        {
            if (IsRunning) throw new InvalidOperationException();
            _job = null;
        }

        public bool IsRunning => _job is {IsDone: false};
    }
}
