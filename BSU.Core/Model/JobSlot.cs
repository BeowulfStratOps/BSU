﻿using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.JobManager;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class JobSlot
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Action<CancellationToken> _action;
        private readonly string _title;
        private readonly IJobManager _jobManager;
        private Task _job;
        private readonly Uid _uid = new Uid();

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
            Logger.Debug($"Creating Simple Job {_title}");
            _job = new SimpleJob(_action, _title, 1).Do(_jobManager);
            await _job;
        }
        
        public bool IsRunning => _job != null && !_job.IsCompleted;
    }
}
