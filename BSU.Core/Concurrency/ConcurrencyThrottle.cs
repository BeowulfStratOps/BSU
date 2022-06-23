using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BSU.Core.Concurrency
{
    public static class ConcurrencyThrottle
    {
        private static readonly SemaphoreSlim Semaphore = new(5); // TODO: get from config

        // This only limits concurrent tasks, it does not provide any parallel execution on its own -> created tasks should be on the threadpool for this to be really useful
        public static async Task Do<T>(IEnumerable<T> workItems, Func<T, Task> taskCreator, CancellationToken cancellationToken)
        {
            var started = new List<Task>();

            async Task StartTaskWithCleanup(T workItem)
            {
                try
                {
                    await taskCreator(workItem);
                }
                finally
                {
                    Semaphore.Release();
                }
            }

            try
            {
                foreach (var workItem in workItems)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Semaphore.WaitAsync(cancellationToken);
                    started.Add(StartTaskWithCleanup(workItem));
                }
            }
            finally
            {
                await Task.WhenAll(started);
            }
        }
    }
}
