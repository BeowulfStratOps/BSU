using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace BSU.Core.Concurrency
{
    public static class TaskHelpers
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static ConfiguredTaskAwaitable DropContext(this Task task) => task.ConfigureAwait(false);
        public static ConfiguredTaskAwaitable<T> DropContext<T>(this Task<T> task) => task.ConfigureAwait(false);

        public static Task AsTask(this CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource();
            cancellationToken.Register(() => tcs.SetResult());
            return tcs.Task;
        }

        public static async Task WithUpdates(this Task task, TimeSpan interval, Action callback)
        {
            await task.WithUpdates(() => Task.Delay(interval), callback);
        }

        // Mostly for testing...
        internal static async Task WithUpdates(this Task task, Func<Task> delayFactory, Action callback)
        {
            while (true)
            {
                await Task.WhenAny(task, delayFactory());
                if (task.IsCompleted)
                {
                    await task;
                    return;
                }
                callback();
            }
        }

        /// <summary>
        /// Continue in an event bus, which is also responsible for catching errors.
        /// TODO: only reason we can't just use the current SynchronizationContext is error handling. if we can reliably log/etc. errors with that, it would be better than passing the bus around
        /// </summary>
        internal static void ContinueInEventBus<T>(this Task<T> task, IEventBus eventBus, Action<Func<T>> continuation)
        {
            task.ContinueWith((taskResult, _) =>
            {
                eventBus.ExecuteSynchronized(() =>
                {
                    continuation(() => taskResult.Result);
                });
            }, null);
        }
    }
}
