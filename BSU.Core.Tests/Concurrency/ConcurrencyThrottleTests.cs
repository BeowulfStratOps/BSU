using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Tests.Util;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace BSU.Core.Tests.Concurrency;

public class ConcurrencyThrottleTest : LoggedTest
{
    public ConcurrencyThrottleTest(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    private class WorkUnit
    {
        private readonly int _delayMs;
        private readonly Action? _onStarted;
        private readonly Action? _onFinished;

        public WorkUnit(int delayMs, Action? onStarted = null, Action? onFinished = null)
        {
            _delayMs = delayMs;
            _onStarted = onStarted;
            _onFinished = onFinished;
        }
        public bool WasCalled { get; private set; }

        public async Task Do()
        {
            WasCalled = true;
            _onStarted?.Invoke();
            await Task.Delay(_delayMs);
            _onFinished?.Invoke();
        }
    }

    [Fact]
    private async Task Success()
    {
        var infos = new List<WorkUnit>
        {
            new(1000),
            new(1000),
            new(1000),
            new(1000),
            new(1000),
            new(1000),
            new(1000),
            new(1000),
            new(1000),
            new(1000),
        };

        var startTime = DateTime.Now;

        await ConcurrencyThrottle.Do(infos, i => i.Do(), CancellationToken.None);

        var duration = (DateTime.Now - startTime).TotalSeconds;
        Assert.True(duration < 3);
        Assert.All(infos, info => Assert.True(info.WasCalled));
    }

    [Fact]
    private async Task Cancel()
    {
        var cts = new CancellationTokenSource();

        var infos = new List<WorkUnit>
        {
            new(2000),
            new(2000),
            new(2000),
            new(2000),
            new(1000, onFinished: cts.Cancel),
            new(1000),
            new(1000),
            new(1000),
            new(1000),
            new(1000),
        };

        var startTime = DateTime.Now;

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => ConcurrencyThrottle.Do(infos, i => i.Do(), cts.Token));

        var duration = (DateTime.Now - startTime).TotalSeconds;
        Assert.True(duration < 3);
        var called = infos.Count(wu => wu.WasCalled);
        if (called > 7)
            throw new AssertActualExpectedException("<=6", called, "Called more than expected");
    }

    [Fact]
    private async Task Exception()
    {
        async Task Test()
        {
            await Task.Delay(100);
            throw new TestException();
        }

        await Test().ContinueWith(async t =>
        {
            await t;
        });

        var cts = new CancellationTokenSource();

        var infos = new List<WorkUnit>
        {
            new(1000),
            new(1000),
            new(1000),
            new(1000),
            new(1000, onFinished: () => throw new TestException()),
            new(1000),
            new(1000),
            new(1000),
            new(1000),
            new(1000),
        };

        var startTime = DateTime.Now;

        await Assert.ThrowsAsync<TestException>(() => ConcurrencyThrottle.Do(infos, i => i.Do(), cts.Token));

        var duration = (DateTime.Now - startTime).TotalSeconds;
        Assert.True(duration < 3);
        Assert.All(infos, info => Assert.True(info.WasCalled));
    }

    [Fact]
    private async Task ManyExceptions()
    {
        async Task Test()
        {
            await Task.Delay(100);
            throw new TestException();
        }

        await Test().ContinueWith(async t =>
        {
            await t;
        });

        var cts = new CancellationTokenSource();

        var infos = new List<WorkUnit>
        {
            new(1000, onFinished: () => throw new TestException()),
            new(1000, onFinished: () => throw new TestException()),
            new(1000, onFinished: () => throw new TestException()),
            new(1000, onFinished: () => throw new TestException()),
            new(1000, onFinished: () => throw new TestException()),
            new(1000),
            new(1000),
            new(1000),
            new(1000),
            new(1000),
        };

        var startTime = DateTime.Now;

        await Assert.ThrowsAsync<TestException>(() => ConcurrencyThrottle.Do(infos, i => i.Do(), cts.Token));

        var duration = (DateTime.Now - startTime).TotalSeconds;
        Assert.True(duration < 3);
        Assert.All(infos, info => Assert.True(info.WasCalled));
    }
}
