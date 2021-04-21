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
        private Task _job;

        internal JobSlot(Action<CancellationToken> action, string title, IJobManager jobManager)
        {
            _action = action;
            _title = title;
            _jobManager = jobManager;
        }

        public async Task Do()
        {
            if (_job != null)
            {
                await _job;
                return;
            }
            _logger.Debug($"Creating Simple Job {_title}");
            _job = new SimpleJob(_action, _title, 1).Do(_jobManager);
            await _job;
        }

        public bool IsRunning => _job != null && !_job.IsCompleted;
    }
}
