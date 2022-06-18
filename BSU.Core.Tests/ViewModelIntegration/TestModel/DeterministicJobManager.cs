using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.ViewModel;
using BSU.CoreCommon;

namespace BSU.Core.Tests.ViewModelIntegration.TestModel;

internal class DeterministicJobManager : IJobManager
{
    private readonly IAsyncVoidExecutor _asyncVoidExecutor;

    public DeterministicJobManager(IAsyncVoidExecutor asyncVoidExecutor)
    {
        _asyncVoidExecutor = asyncVoidExecutor;
    }

    // ReSharper disable once NotAccessedPositionalProperty.Local
    private record RunningJob(string JobName, Guid Guid);

    private readonly HashSet<RunningJob> _runningJobs = new();

    private RunningJob AddJob(string jobName)
    {
        var runningJob = new RunningJob(jobName, Guid.NewGuid());
        _runningJobs.Add(runningJob);
        return runningJob;
    }

    public async Task Run(string jobName, Func<Task> action, CancellationToken cancellationToken)
    {
        var job = AddJob(jobName);
        try
        {
            await action();
        }
        finally
        {
            _runningJobs.Remove(job);
        }
    }

    public async Task<T> Run<T>(string jobName, Func<Task<T>> action, CancellationToken cancellationToken)
    {
        var job = AddJob(jobName);
        try
        {
            return await action();
        }
        finally
        {
            _runningJobs.Remove(job);
        }
    }

    public void Run<T>(string jobName, Func<Task<T>> action, Action<Func<T>> synchronizedContinuation, CancellationToken cancellationToken)
    {
        var job = AddJob(jobName);
        _asyncVoidExecutor.Execute(async () =>
        {
            var task = action();
            try
            {
                await task;
            }
            catch (Exception)
            {
                // just waiting
            }

            _runningJobs.Remove(job);
            T GetResult() => task.GetAwaiter().GetResult();
            synchronizedContinuation(GetResult);
        });
    }

    public List<string> GetRunningJobs() => _runningJobs.Select(r => r.JobName).ToList();
}
