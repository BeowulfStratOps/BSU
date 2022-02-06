using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace BSU.Core.Concurrency
{
    public static class TaskHelpers
    {
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
        /// Continue in dispatcher, which is also responsible for catching errors.
        /// </summary>
        internal static void ContinueInDispatcher<T>(this Task<T> task, IDispatcher dispatcher, Action<Func<T>> continuation)
        {
            task.ContinueWith((taskResult, _) =>
            {
                dispatcher.ExecuteSynchronized(() =>
                {
                    continuation(() => taskResult.Result);
                });
            }, null);
        }
    }
}
