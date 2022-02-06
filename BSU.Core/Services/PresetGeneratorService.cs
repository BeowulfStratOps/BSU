using System;
using BSU.Core.Events;
using BSU.Core.Ioc;
using BSU.Core.Model;
using NLog;

namespace BSU.Core.Services;

internal class PresetGeneratorService
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IModel _model;
    private readonly IRepositoryStateService _stateService;

    public PresetGeneratorService(IServiceProvider serviceProvider)
    {
        var eventManager = serviceProvider.Get<IEventManager>();
        _model = serviceProvider.Get<IModel>();
        _stateService = serviceProvider.Get<IRepositoryStateService>();
        eventManager.Subscribe<CalculatedStateChangedEvent>(evt => CheckRepository(evt.Repository));
        eventManager.Subscribe<SettingsChangedEvent>(evt => CheckRepository(evt.Repository));
    }

    private void CheckRepository(IModelRepository repository)
    {
        if (repository.Settings.UseBsuLauncher) return;
        var state = _stateService.GetStateFor(repository);
        if (state != CalculatedRepositoryStateEnum.Ready && state != CalculatedRepositoryStateEnum.ReadyPartial) return;

        // TODO: only generate if it's different from the last generated preset / related arma launcher preset
        // TODO: should we show a notification that it was created?
        // TODO: should we warn if the launcher is currently open and generation didn't work?
        // TODO: generate preset.
    }
}
