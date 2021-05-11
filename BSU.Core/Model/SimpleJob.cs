﻿using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.JobManager;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class SimpleJob : IJob
    {
        private readonly Logger _logger = EntityLogger.GetLogger();

        private readonly Action<CancellationToken> _action;
        private readonly string _title;
        private readonly int _priority;
        private readonly CancellationTokenSource _tokenSource = new();
        private readonly object _lock = new ();
        private bool _workAssgined;

        public SimpleJob(Action<CancellationToken> action, string title, int priority)
        {
            _action = action;
            _title = title;
            _priority = priority;
        }

        public void Abort()
        {
            _tokenSource.Cancel();
        }

        public bool DoWork(IActionQueue actionQueue)
        {
            lock (_lock)
            {
                if (_workAssgined) return false;
                _workAssgined = true;
            }
            try
            {
                _action(_tokenSource.Token);
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
            IsDone = true;
            Done?.Invoke();
            return false;
        }

        public bool IsDone { get; private set; }
        public event Action Done;

        public string GetTitle() => _title;
        public int GetPriority() => _priority;
        public int GetUid() => _logger.GetId();
    }
}
