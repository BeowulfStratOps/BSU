using System;
using BSU.Core.Events;
using BSU.Core.Ioc;
using BSU.Core.Model;
using NLog;

namespace BSU.Core.Services;

internal class PresetGenerator
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public PresetGenerator(IServiceProvider serviceProvider)
    {
        serviceProvider.Get<IEventManager>().Subscribe<AnythingChangedEvent>(OnAnyChange);
    }

    private void OnAnyChange(AnythingChangedEvent evt)
    {
        // TODO: only check presets that have the setting enabled
        // TODO: generate preset only when the state changes to a ready or partial ready state i guess?
        // TODO: or when it is in ready state, but the setting is changed to using arma launcher -> need a new event for that I think
        // TODO: only generate if it's different from the last generated preset / related arma launcher preset
        // TODO: should we show a notification that it was created?
        // TODO: should we warn if the launcher is currently open and generation didn't work?
        // TODO: generate preset.
    }
}
