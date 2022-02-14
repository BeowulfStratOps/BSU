using System;
using System.Threading.Tasks;
using BSU.Core.Events;
using BSU.Core.ViewModel;
using NLog;

namespace BSU.Core.Tests.ActionBased.TestModel;

internal class TestAsyncVoidExecutor : IAsyncVoidExecutor
{
    private readonly IEventManager _eventManager;
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    public TestAsyncVoidExecutor(IEventManager eventManager)
    {
        _eventManager = eventManager;
    }

    public async void Execute(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception e)
        {
            Logger.Error(e);
            _eventManager.Publish(new ErrorEvent(e.Message));
            throw;
        }
    }
}