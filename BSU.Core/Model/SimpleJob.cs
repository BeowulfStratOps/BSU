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

        public Uid GetUid() => _uid;

        public bool DoWork()
        {
            if (_done) return false;
            try
            {
                _action(_tokenSource.Token);
            }
            catch (Exception e)
            {
                Error = e;
                Logger.Error(e);
            }
            _done = true;
            OnFinished?.Invoke();
            return false;
        }

        public event Action OnFinished;
        
        public string GetTitle() => _title;
        public int GetPriority() => _priority;
    }
}
