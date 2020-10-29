using System;
using System.Threading;
using BSU.Core.JobManager;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class SimpleJob : IJob
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Action<CancellationToken> _action;
        private readonly string _title;
        private readonly int _priority;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        public Exception Error { get; private set; }
        private readonly  Uid _uid = new Uid();
        private bool _done = false;
        private readonly object _lock = new object();
        private bool _wasShutdownCold = false;

        public SimpleJob(Action<CancellationToken> action, string title, int priority)
        {
            _action = action;
            _title = title;
            _priority = priority;
        }

        public void Abort(bool coldShutdown = false)
        {
            _tokenSource.Cancel();
            _wasShutdownCold |= coldShutdown;
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
                _action(_tokenSource.Token);
            }
            catch (Exception e)
            {
                Error = e;
                Logger.Error(e);
            }
            if (!_wasShutdownCold)
                actionQueue.EnQueueAction(() => OnFinished?.Invoke());
            return false;
        }

        public Exception GetError() => Error;

        public event Action OnFinished;
        
        public string GetTitle() => _title;
        public int GetPriority() => _priority;
    }
}
