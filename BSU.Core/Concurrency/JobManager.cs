using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;

namespace BSU.Core.Concurrency;

public class JobManager : IJobManager
{
    private readonly IDispatcher _dispatcher;

    public JobManager(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public Task Run(string jobName, Func<Task> action, CancellationToken cancellationToken) => Task.Run(action, cancellationToken);

    public Task<T> Run<T>(string jobName, Func<Task<T>> action, CancellationToken cancellationToken) => Task.Run(action, cancellationToken);

    public void Run<T>(string jobName, Func<Task<T>> action, Action<Func<T>> synchronizedContinuation, CancellationToken cancellationToken)
    {
        var task = Task.Run(action, cancellationToken); 
        task.ContinueInDispatcher(_dispatcher, synchronizedContinuation);
    }
}
