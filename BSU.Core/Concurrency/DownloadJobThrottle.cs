using System;
using System.Threading;
using System.Threading.Tasks;

namespace BSU.Core.Concurrency
{
    public static class ConcurrencyThrottle
    {
        private static readonly SemaphoreSlim Semaphore = new(5); // TODO: get from config

        public static async Task<T> Do<T>(Func<Task<T>> action)
        {
            await Semaphore.WaitAsync();
            try
            {
                return await action();
            }
            finally
            {
                Semaphore.Release();
            }
        }
    }
}
