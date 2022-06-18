using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BSU.Core.Events;
using BSU.Core.ViewModel;
using NLog;

namespace BSU.Core.Tests.ActionBased.TestModel;

internal class TestAsyncVoidExecutor : IAsyncVoidExecutor
{
    private readonly IEventManager _eventManager;
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly List<Task> _tasks = new();

    public TestAsyncVoidExecutor(IEventManager eventManager)
    {
        _eventManager = eventManager;
    }

    public async void Execute(Func<Task> action)
    {
        try
        {
            var task = action();
            _tasks.Add(task);
            await task;
        }
        catch (Exception e)
        {
            Logger.Error(e);
            _eventManager.Publish(new ErrorEvent(e.Message));
            throw;
        }
    }

    public async Task<bool> WaitForRunningTasks()
    {
        var waitAll = Task.WhenAll(_tasks);
        await Task.WhenAny(waitAll, Task.Delay(1000));
        return waitAll.IsCompleted;
    }
}
