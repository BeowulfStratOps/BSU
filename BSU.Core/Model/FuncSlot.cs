using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.JobManager;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class FuncSlot<T>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Func<CancellationToken, T> _func;
        private readonly string _title;
        private readonly IJobManager _jobManager;
        private Task<T> _job;
        private readonly Uid _uid = new Uid();

        internal FuncSlot(Func<CancellationToken, T> func, string title, IJobManager jobManager)
        {
            _func = func;
            _title = title;
            _jobManager = jobManager;
        }

        public async Task<T> Do()
        {
            if (_job != null) return await _job;
            Logger.Debug($"Creating Simple Job {_title}");
            _job = new SimpleResultJob<T>(_func, _title, 1).Do(_jobManager);
            return await _job;
        }

        public bool IsRunning => _job != null && !_job.IsCompleted;
    }
}
