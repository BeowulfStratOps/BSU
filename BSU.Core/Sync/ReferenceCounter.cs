using System;

namespace BSU.Core.Sync
{
    internal class ReferenceCounter
    {
        private int _counter;
        private readonly object _lock = new object();

        public ReferenceCounter(int initial)
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
        
        public bool Dec()
        {
            lock (_lock)
            {
                _counter--;
                return _counter <= 0;
            }
        }

        public int Remaining => _counter;
    }
}