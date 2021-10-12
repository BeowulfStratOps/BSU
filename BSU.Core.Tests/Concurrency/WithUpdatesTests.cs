using System;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests.Concurrency
{
    public class WithUpdatesTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public WithUpdatesTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        private class Delays
        {
            private TaskCompletionSource _current = new();

            public Task Get()
            {
                return _current.Task;
            }

            public void Set()
            {
                var old = _current;
                _current = new TaskCompletionSource();
                old.SetResult();
            }
        }

        [Fact]
        public async Task Success()
        {
            var taskTcs = new TaskCompletionSource();
            var delays = new Delays();

            var counter = 0;
            var newTask = taskTcs.Task.WithUpdates(delays.Get, () => counter++);

            delays.Set();
            delays.Set();
            delays.Set();
            taskTcs.SetResult();

            await newTask;

            Assert.Equal(3, counter);
            Assert.Equal(TaskStatus.RanToCompletion, newTask.Status);
        }

        [Fact]
        public async Task Fault()
        {
            var taskTcs = new TaskCompletionSource();
            var delays = new Delays();

            async Task Func()
            {
                await taskTcs.Task;
                throw new InvalidOperationException();
            }

            var task = Func();
            var counter = 0;
            var newTask = task.WithUpdates(delays.Get, () => counter++);

            delays.Set();
            delays.Set();
            delays.Set();
            taskTcs.SetResult();

            await Assert.ThrowsAsync<InvalidOperationException>(() => newTask);
            Assert.Equal(TaskStatus.Faulted, newTask.Status);
            Assert.Equal(3, counter);
        }

        [Fact]
        public async Task Cancel()
        {
            var taskTcs = new TaskCompletionSource();
            var delays = new Delays();

            async Task Func()
            {
                await taskTcs.Task;
            }

            var task = Func();

            var counter = 0;
            var newTask = task.WithUpdates(delays.Get, () => counter++);

            delays.Set();
            delays.Set();
            delays.Set();
            taskTcs.SetCanceled();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => newTask);
            Assert.Equal(TaskStatus.Canceled, newTask.Status);
            Assert.Equal(3, counter);
        }
    }
}
