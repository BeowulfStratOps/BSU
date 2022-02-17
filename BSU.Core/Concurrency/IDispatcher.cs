using System;

namespace BSU.Core.Concurrency
{
    public interface IDispatcher
    {
        void ExecuteSynchronized(Action action);
    }
}
