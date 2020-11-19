using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.JobManager;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class SimpleResultJob<T> : IJob
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly Func<CancellationToken, T> _action;
        private readonly string _title;
        private readonly int _priority;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private readonly  Uid _uid = new Uid();
        private bool _done = false;
        private readonly object _lock = new object();
        
        private readonly TaskCompletionSource<T> _tcs = new TaskCompletionSource<T>(); 

        public SimpleResultJob(Func<CancellationToken, T> action, string title, int priority)
        {
            _action = action;
            _title = title;
            _priority = priority;
        }

        public void Abort()
        {
            _tokenSource.Cancel();
        }

        public Uid GetUid() => _uid;

        public bool DoWork(IActionQueue actionQueue)
        {
            lock (_lock)
            {
                if (_done) return false;
                _done = true;
            }
            try
            {
                var result = _action(_tokenSource.Token);
                _tcs.SetResult(result);
            }
            catch (Exception e)
            {
                _logger.Error(e);
                _tcs.SetException(e);
            }
            return false;
        }

        public Task<T> Do(IJobManager jobManager)
        {
            jobManager.QueueJob(this);
            return _tcs.Task;
        }
        
        public string GetTitle() => _title;
        public int GetPriority() => _priority;
    }
}
