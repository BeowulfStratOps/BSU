using System;
using System.Collections.Generic;
using System.Threading;
using BSU.Core.Concurrency;

namespace BSU.Core.Tests.ActionBased.TestModel
{
    internal class TestDispatcher : IDispatcher
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

        public void WaitForWork()
        {
            while (_todo.Count == 0)
            {
                Thread.Sleep(1);
            }
        }

        public void Work(int timeOutMs, Func<bool>? done = null)
        {
            for (var i = 0; i < timeOutMs; i++)
            {
                Work();
                if (done != null && done()) return;
                Thread.Sleep(1);
            }

            if (done != null && !done())
                throw new TimeoutException();
        }
    }
}
