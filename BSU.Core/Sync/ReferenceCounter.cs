using System;

namespace BSU.Core.Sync
{
    internal class ReferenceCounter
    {
        private int _counter = 0;
        private bool _started;
        public bool Done { private set; get; }
        public event Action OnDone;
        private readonly object _lock = new object();

        public void Inc()
        {
            lock (_lock)
            {
                _counter++;
                _started = true;
            }
        }
        
        public void Dec()
        {
            lock (_lock)
            {
                _counter--;
                if (_counter != 0 || !_started) return;
                Done = true;
                OnDone?.Invoke();
            }
        }
    }
}