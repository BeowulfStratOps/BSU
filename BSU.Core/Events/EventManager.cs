using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;

namespace BSU.Core.Events;

public interface IEventManager
{
    void Subscribe<T>(Action<T> handler);
    void Unsubscribe<T>(Action<T> handler);
    void Publish<T>(T @event);
}

public class EventManager : IEventManager
{
    private readonly Dictionary<Type, List<object>> _handlers = new();
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    private const double TimeLimitMs = 50;

    public void Subscribe<T>(Action<T> handler)
    {
        if (!_handlers.TryGetValue(typeof(T), out var list))
        {
            list = new List<object>();
            _handlers.Add(typeof(T), list);
        }

        if (list.Contains(handler))
            throw new ArgumentException("Handler already present");
        list.Add(handler);
    }

    public void Unsubscribe<T>(Action<T> handler)
    {
        if (!_handlers.TryGetValue(typeof(T), out var list) || !list.Remove(handler))
            throw new ArgumentException($"Couldn't find handler {handler}");
    }

    public void Publish<T>(T evt)
    {
        if (!_handlers.TryGetValue(typeof(T), out var list))
        {
            _logger.Info($"No handlers for {evt}.");
            return;
        }

        var stopWatch = new Stopwatch();
        stopWatch.Start();

        var guid = Guid.NewGuid();
        _logger.Trace($"Executing {list.Count} handler(s) for  event {evt}-{guid}.");
        foreach (var handler in list.Cast<Action<T>>())
        {
            handler(@evt);
        }
        _logger.Trace($"Done handling event {evt}-{guid}");

        stopWatch.Stop();
        if (stopWatch.ElapsedMilliseconds < TimeLimitMs) return;
        _logger.Warn($"GUI Lag: Handling event {evt}-{guid} took {stopWatch.ElapsedMilliseconds}ms. {list.Count} handlers.");
    }
}
