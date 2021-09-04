using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Tests.Util;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests.Concurrency
{
    // TODO: test all the cancellation stuff
    public class ResettableLazyAsyncTests : LoggedTest
    {
        public ResettableLazyAsyncTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public async Task Simple()
        {
            var lazy = new ResettableLazyAsync<string>(async ct => "ok");

            var result = await lazy.GetAsync(CancellationToken.None);
            Assert.Equal("ok", result);
        }

        [Fact]
        public async Task Caching()
        {
            var callCounter = 0;
            var lazy = new ResettableLazyAsync<string>(async ct =>
            {
                callCounter++;
                return "ok";
            });

            var result = await lazy.GetAsync(CancellationToken.None);
            Assert.Equal("ok", result);
            Assert.Equal(1, callCounter);
            result = await lazy.GetAsync(CancellationToken.None);
            Assert.Equal("ok", result);
            Assert.Equal(1, callCounter);
        }

        [Fact]
        public async Task ConcurrentCaching()
        {
            var callCounter = 0;
            var lazy = new ResettableLazyAsync<string>(async ct =>
            {
                callCounter++;
                await Task.Delay(TimeSpan.FromSeconds(1));
                return "ok";
            });

            var resultTask1 = lazy.GetAsync(CancellationToken.None);
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            var resultTask2 = lazy.GetAsync(CancellationToken.None);
            await Task.WhenAll(resultTask1, resultTask2);
            Assert.Equal("ok", resultTask1.Result);
            Assert.Equal("ok", resultTask2.Result);
            Assert.Equal(1, callCounter);
        }

        [Fact]
        public async Task CancelFirstGet()
        {
            var lazy = new ResettableLazyAsync<string>(async ct =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
                return "ok";
            });

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.1));
            var task1 = lazy.GetAsync(cts.Token);

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await task1);
        }

        [Fact]
        public async Task CancelSecondGet()
        {
            var lazy = new ResettableLazyAsync<string>(async ct =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
                return "ok";
            });

            var task1 = lazy.GetAsync(CancellationToken.None);
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.1));
            var task2 = lazy.GetAsync(cts.Token);
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await task2);

            Assert.Equal("ok", await task1);
        }

        [Fact]
        public async Task LockDuration()
        {
            var lazy = new ResettableLazyAsync<string>(async ct =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
                return "ok";
            });

            var start = DateTime.Now;

            lazy.GetAsync(CancellationToken.None);
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.1));
            var task2 = lazy.GetAsync(cts.Token);
            try
            {
                await task2;
            }
            catch (OperationCanceledException)
            {
            }

            Assert.InRange((DateTime.Now - start).TotalSeconds, 0.55, 0.65);
        }

        [Fact]
        public async Task Set()
        {
            var lazy = new ResettableLazyAsync<string>(async ct =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
                return "ok";
            });
            await lazy.Set("set");

            Assert.Equal("set", await lazy.GetAsync(CancellationToken.None));
        }

        [Fact]
        public async Task ResetSet()
        {
            var lazy = new ResettableLazyAsync<string>(async ct =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
                return "ok";
            });

            await lazy.ResetAndWaitAsync();
            await lazy.Set("set");

            Assert.Equal("set", await lazy.GetAsync(CancellationToken.None));
        }

        [Fact]
        public async Task RunResetSet()
        {
            var lazy = new ResettableLazyAsync<string>(async ct =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
                return "ok";
            });

            await lazy.GetAsync(CancellationToken.None);
            await lazy.ResetAndWaitAsync();
            await lazy.Set("set");

            Assert.Equal("set", await lazy.GetAsync(CancellationToken.None));
        }
    }
}
