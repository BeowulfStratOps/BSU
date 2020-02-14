using System;

namespace BSU.Core.Sync
{
    internal class ReferenceCounter
    {
        private int _counter;
        public bool Done { private set; get; }
        public event Action OnDone;
        private readonly object _lock = new object();

        public ReferenceCounter(int initial = 0)
        {
            _counter = initial;
        }
        
        public void Inc()
        {
            lock (_lock)
            {
                _counter++;
            }
        }
        
        public void Dec()
        {
            lock (_lock)
            {
                _counter--;
                if (_counter != 0) return;
                Done = true;
                OnDone?.Invoke();
            }
        }

        public int Remaining => _counter;
    }
}