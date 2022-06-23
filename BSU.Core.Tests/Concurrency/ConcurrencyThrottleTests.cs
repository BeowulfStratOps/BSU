using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Tests.Util;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests.Concurrency;

public class ConcurrencyThrottleTest : LoggedTest
{
    public ConcurrencyThrottleTest(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    private class WorkUnit
    {
        private readonly Func<Task> _createTask;
        public Task? Task;

        public WorkUnit(Func<Task> createTask)
        {
            _createTask = createTask;
        }

        public Task Do()
        {
            
            if (Task != null)
                throw new InvalidOperationException();
            Task = _createTask();
            return Task;
        }
    }

    [Fact]
    private async Task Success()
    {
        var tcs = new TaskCompletionSource();
        
        var infos = new List<WorkUnit>
        {
            new(() => tcs.Task),
            new(() => tcs.Task),
            new(() => tcs.Task),
            new(() => tcs.Task),
            new(() => tcs.Task),
            new(() => tcs.Task),
            new(() => tcs.Task),
            new(() => tcs.Task),
            new(() => tcs.Task),
            new(() => tcs.Task),
            new(() => tcs.Task),
            new(() => tcs.Task),
        };

        var throttleTask = ConcurrencyThrottle.Do(infos, i => i.Do(), CancellationToken.None);

        Assert.False(throttleTask.IsCompleted);
        tcs.SetResult();

        await throttleTask;

        Assert.All(infos, info => Assert.True(info.Task is { IsCompletedSuccessfully: true }));
    }

    [Fact]
    private async Task Cancel()
    {
        var tcs1 = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();

        var infos = new List<WorkUnit>
        {
            new(() => tcs1.Task),
            new(() => tcs1.Task),
            new(() => tcs1.Task),
            new(() => tcs2.Task),
            new(() => tcs2.Task),
            new(() => tcs2.Task),
            new(() => tcs2.Task),
            new(() => tcs2.Task),
            new(() => tcs2.Task),
            new(() => tcs2.Task),
            new(() => tcs1.Task),
            new(() => tcs1.Task),
        };

        var cts = new CancellationTokenSource();

        var throttleTask = ConcurrencyThrottle.Do(infos, i => i.Do(), cts.Token);
        
        tcs1.SetResult();
        cts.Cancel();
        tcs2.SetCanceled(cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => throttleTask);

        var succeeded = infos.Count(wu => wu.Task?.IsCompletedSuccessfully ?? false);
        Assert.Equal(3, succeeded);
    }

    [Fact]
    private async Task Exceptions()
    {
        var infos = new List<WorkUnit>
        {
            new(() => Task.CompletedTask),
            new(() => Task.CompletedTask),
            new(() => Task.FromException(new TestException())),
            new(() => Task.FromException(new TestException())),
            new(() => Task.FromException(new TestException())),
            new(() => Task.FromException(new TestException())),
            new(() => Task.FromException(new TestException())),
            new(() => Task.CompletedTask),
            new(() => Task.CompletedTask),
            new(() => Task.CompletedTask),
            new(() => Task.CompletedTask),
            new(() => Task.CompletedTask)
        };

        var throttleTask = ConcurrencyThrottle.Do(infos, i => i.Do(), CancellationToken.None);
        await Assert.ThrowsAnyAsync<Exception>(() => throttleTask);

        Assert.Equal(7, infos.Count(i => i.Task!.IsCompletedSuccessfully));
    }
}
