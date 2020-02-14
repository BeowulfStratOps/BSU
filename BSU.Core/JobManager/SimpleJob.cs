using System;
using System.Threading;
using BSU.Core.JobManager;
using BSU.Core.Sync;
using BSU.CoreCommon;

namespace BSU.Core.View
{
    internal class SimpleJob : IJob
    {
        private readonly string _title;
        private readonly int _priority;
        private readonly WorkUnit _work;
        private bool _queued;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private Exception _error;
        private readonly  Uid _uid = new Uid();
        private readonly ReferenceCounter _counter;

        public SimpleJob(Action action, string title, int priority)
        {
            _title = title;
            _priority = priority;
            _counter = new ReferenceCounter(1);
            _counter.OnDone += () => { OnFinished?.Invoke(); };
            _work = new SimpleWorkUnit(action, _tokenSource.Token);
        }

        public void Abort()
        {
            _tokenSource.Cancel();
        }

        public Uid GetUid() => _uid;

        public WorkUnit GetWork()
        {
            if (_queued) return null;
            _queued = true;
            return _work;
        }

        public void WorkItemFinished()
        {
            _counter.Dec();
        }

        public bool IsDone() => _work.IsDone();

        public void SetError(Exception e)
        {
            _error = e;
        }

        public event Action OnFinished;
        public string GetTitle() => _title;

        public event Action Progress;

        public float GetProgress() => 1 - _counter.Remaining;

        public int GetPriority() => _priority;
    }
}
