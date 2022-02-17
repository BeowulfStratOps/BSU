using System;
using System.Threading;
using System.Windows.Threading;
using BSU.Core.Concurrency;
using NLog;

namespace BSU.GUI;

public class SimpleDispatcher : IDispatcher
{
    private readonly Dispatcher _baseDispatcher;
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    public SimpleDispatcher(Dispatcher baseDispatcher)
    {
        _baseDispatcher = baseDispatcher;
        _baseDispatcher.UnhandledException += BaseDispatcherOnUnhandledException;
    }

    private void BaseDispatcherOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger.Error(e.Exception);
    }

    public void ExecuteSynchronized(Action action)
    {
        _baseDispatcher.InvokeAsync(action, DispatcherPriority.Background);
    }
}
