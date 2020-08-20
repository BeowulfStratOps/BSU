using System;

namespace BSU.Core.Model.Utility
{
    public class Promise<T>
    {
        // TODO: replace with Task<T>

        public T Value { get; private set; }
        public Exception Exception { get; private set; }
        
        public bool HasValue { get; private set; }
        public bool HasError { get; private set; }
        
        public event Action OnValue;
        public event Action OnError; 
        
        private readonly object _lock = new object();
        
        public void Error(Exception e)
        {
            lock (_lock)
            {
                if (HasValue) throw new InvalidOperationException();
                HasError = true;
            }
            Exception = e;
            OnError?.Invoke();
        }
        
        public void Set(T value)
        {
            lock (_lock)
            {
                if (HasError) throw new InvalidOperationException();
                HasValue = true;
            }
            Value = value;
            OnValue?.Invoke();
        }
    }
}