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
            while (true)
            {
                await Task.WhenAny(task, Task.Delay(interval));
                if (task.IsCompleted)
                {
                    await task;
                    return;
                }
                callback();
            }
        }
    }
}
