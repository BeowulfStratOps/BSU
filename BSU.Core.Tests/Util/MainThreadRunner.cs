using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BSU.Core.Tests.Util;

public static class MainThreadRunner
{
    public static void Run(Func<Task> func)
    {
        Thread.CurrentThread.Name = "main";
        var sc = new CollectingSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(sc);
        var task = func();
        while (!task.IsCompleted)
        {
            sc.Work();
            Thread.Sleep(1);
        }
        task.GetAwaiter().GetResult();
    }

    private class CollectingSynchronizationContext : SynchronizationContext
    {
        private readonly ConcurrentQueue<(SendOrPostCallback action, object? state)> _actions = new();

        public override SynchronizationContext CreateCopy()
        {
            throw new NotImplementedException();
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            _actions.Enqueue((d, state));
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            _actions.Enqueue((d, state));
        }

        public void Work()
        {
            while (_actions.TryDequeue(out var v))
            {
                var (action, state) = v;

                var stopWatch = new Stopwatch();
                stopWatch.Start();

                action(state);

                stopWatch.Stop();
#if !DEBUG
                if (stopWatch.ElapsedMilliseconds > 16) // we expect 60fps all the time.
                    throw new Exception("Freeze detected!");
#endif
            }
        }
    }
}
