using System;
using System.Threading;
using System.Threading.Tasks;

namespace BSU.Core.Concurrency
{
    public static class ConcurrencyThrottle
    {
        private static readonly SemaphoreSlim Semaphore = new(5); // TODO: get from config

        public static async Task Do(Func<Task> action, CancellationToken cancellationToken)
        {
            await Semaphore.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await action();
            }
            finally
            {
                Semaphore.Release();
            }
        }
    }
}
