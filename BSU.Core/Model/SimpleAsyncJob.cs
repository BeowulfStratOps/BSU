using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.JobManager;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class SimpleAsyncJob : IJob
    {
        private readonly Logger _logger = EntityLogger.GetLogger();

        private readonly Action<CancellationToken> _action;
        private readonly string _title;
        private readonly int _priority;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private bool _done = false;
        private readonly object _lock = new object();
        private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();

        public SimpleAsyncJob(Action<CancellationToken> action, string title, int priority)
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
                if (_done) return false;
                _done = true;
            }
            try
            {
                _action(_tokenSource.Token);
                _tcs.SetResult(null);
            }
            catch (Exception e)
            {
                _logger.Error(e);
                _tcs.SetException(e);
            }
            return false;
        }

        public Task Do(IJobManager jobManager)
        {
            jobManager.QueueJob(this);
            return _tcs.Task;
        }

        public string GetTitle() => _title;
        public int GetPriority() => _priority;
        public int GetUid() => _logger.GetId();
    }
}
