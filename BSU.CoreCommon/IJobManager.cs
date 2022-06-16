using System;
using System.Threading;
using System.Threading.Tasks;

namespace BSU.CoreCommon;

public interface IJobManager
{
    public Task Run(Func<Task> action, CancellationToken cancellationToken);
    public Task<T> Run<T>(Func<Task<T>> action, CancellationToken cancellationToken);
    public void Run<T>(Func<Task<T>> action, Action<Func<T>> synchronizedContinuation, CancellationToken cancellationToken);
}
