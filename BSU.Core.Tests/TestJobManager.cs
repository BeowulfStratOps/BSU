using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;

namespace BSU.Core.Tests;

internal class TestJobManager : IJobManager
{
    // TODO: wrap canceled exceptions in tasks?
    public Task Run(Func<Task> action, CancellationToken cancellationToken)
    {
        var task = action();
        task.Wait(cancellationToken);
        return task;
    }

    public Task<T> Run<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public void Run<T>(Func<Task<T>> action, Action<Func<T>> synchronizedContinuation, CancellationToken cancellationToken)
    {
        var task = action();
        task.Wait(cancellationToken);
        synchronizedContinuation(() => task.Result);
    }
}
