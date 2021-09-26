using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using Xunit;

namespace BSU.Core.Tests.Concurrency
{
    public class WithUpdatesTests
    {
        [Fact]
        public async Task Success()
        {
            var task = Task.Delay(1100);
            var counter = 0;
            var newTask = task.WithUpdates(TimeSpan.FromMilliseconds(200), () => counter++);
            await newTask;
            Assert.Equal(5, counter);
            Assert.Equal(TaskStatus.RanToCompletion, newTask.Status);
        }

        [Fact]
        public async Task Fault()
        {
            async Task Func()
            {
                await Task.Delay(700);
                throw new InvalidOperationException();
            }

            var task = Func();
            var counter = 0;
            var newTask = task.WithUpdates(TimeSpan.FromMilliseconds(200), () => counter++);
            await Assert.ThrowsAsync<InvalidOperationException>(() => newTask);
            Assert.Equal(TaskStatus.Faulted, newTask.Status);
            Assert.Equal(3, counter);
        }

        [Fact]
        public async Task Cancel()
        {
            var cts = new CancellationTokenSource(500);
            var task = Task.Delay(550, cts.Token);
            var counter = 0;
            var newTask = task.WithUpdates(TimeSpan.FromMilliseconds(200), () => counter++);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => newTask);
            Assert.Equal(TaskStatus.Canceled, newTask.Status);
            Assert.Equal(2, counter);
        }
    }
}
