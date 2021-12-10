using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Tests.Mocks;
using Xunit;

namespace BSU.Core.Tests.Util;

public class SingleThreadRunnerSelfTests
{
    [Fact]
    private void SynchronizationContextSelfTest()
    {
        // checking that it uses the correct thread and goes back to it. and error handling.

        async Task Func()
        {
            var threadId = Environment.CurrentManagedThreadId;

            await Task.Yield();

            Assert.Equal(threadId, Environment.CurrentManagedThreadId);

            await Task.Delay(1);

            Assert.Equal(threadId, Environment.CurrentManagedThreadId);

            await Task.Run(() =>
            {
                Thread.Sleep(2);
                Assert.NotEqual(threadId, Environment.CurrentManagedThreadId);
            });

            Assert.Equal(threadId, Environment.CurrentManagedThreadId);

            throw new TestException();
        }

        Assert.Throws<TestException>(() =>
        {
            MainThreadRunner.Run(Func);
        });
    }

    [Fact]
    private void SynchronizationContextSelfTest2()
    {
        // just checking that it runs concurrent stuff reasonably (could use a better test/understanding)
        ConcurrentQueue<int> result = new();
        async Task Func()
        {
            for (int i = 0; i < 10; i++)
            {
                result.Enqueue(i);
                Thread.Sleep(50);
                await Task.Yield();
            }
        }
        MainThreadRunner.Run(() => Task.WhenAll(Func(), Func()));
        var expected = Enumerable.Range(0, 10).SelectMany(v => new[] { v, v }).ToList();
        Assert.Equal(expected, result.ToList());
    }
}
