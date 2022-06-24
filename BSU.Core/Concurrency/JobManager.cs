using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Concurrency;

public class JobManager : IJobManager
{
    private readonly IDispatcher _dispatcher;
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    public JobManager(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public Task Run(string jobName, Func<Task> action, CancellationToken cancellationToken)
    {
        _logger.Debug($"Queueing job {jobName}");
        return Task.Run(async () =>
        {
            _logger.Debug($"Starting job {jobName}");
            try
            {
                await action();
            }
            finally
            {
                _logger.Debug($"Finished job {jobName}");
            }
        }, cancellationToken);
    }

    public Task<T> Run<T>(string jobName, Func<Task<T>> action, CancellationToken cancellationToken)
    {
        _logger.Debug($"Queueing job {jobName}");
        return Task.Run(async () =>
        {
            _logger.Debug($"Starting job {jobName}");
            try
            {
                return await action();
            }
            finally
            {
                _logger.Debug($"Finished job {jobName}");
            }
        }, cancellationToken);
    }

    public void Run<T>(string jobName, Func<Task<T>> action, Action<Func<T>> synchronizedContinuation, CancellationToken cancellationToken)
    {
        _logger.Debug($"Queueing job {jobName}");
        var task = Task.Run(async () =>
        {
            _logger.Debug($"Starting job {jobName}");
            try
            {
                return await action();
            }
            finally
            {
                _logger.Debug($"Finished job {jobName}");
            }

        }, cancellationToken); 
        task.ContinueInDispatcher(_dispatcher, synchronizedContinuation);
    }
}
