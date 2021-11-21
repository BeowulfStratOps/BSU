using System;
using System.Collections.Generic;
using System.Threading;
using BSU.Core.Concurrency;

namespace BSU.Core.Tests.Mocks
{
    internal class TestEventBus : IEventBus
    {
        private readonly Queue<Action> _todo = new();

        public void ExecuteSynchronized(Action action)
        {
            _todo.Enqueue(action);
        }

        public void Work()
        {
            while (_todo.TryDequeue(out var action))
            {
                action();
            }
        }

        public void WorkUntil(int timeOutMs, Func<bool> done = null)
        {
            for (var i = 0; i < timeOutMs; i++)
            {
                Work();
                if (done != null && done()) return;
                Thread.Sleep(1);
            }
        }
    }
}
