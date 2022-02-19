using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.Core.Concurrency;
using BSU.Core.Events;
using BSU.Core.Ioc;
using BSU.Core.Launch;
using BSU.Core.Model;
using NLog;

namespace BSU.Core.Services;

internal class PresetGeneratorService
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IRepositoryStateService _stateService;
    private readonly IEventManager _eventManager;
    private readonly IDispatcher _dispatcher;

    public PresetGeneratorService(IServiceProvider serviceProvider)
    {
        var eventManager = serviceProvider.Get<IEventManager>();
        _stateService = serviceProvider.Get<IRepositoryStateService>();
        _eventManager = serviceProvider.Get<IEventManager>();
        _dispatcher = serviceProvider.Get<IDispatcher>();
        eventManager.Subscribe<CalculatedStateChangedEvent>(evt => CheckRepository(evt.Repository));
        eventManager.Subscribe<SettingsChangedEvent>(evt => CheckRepository(evt.Repository));
    }

    private string SanitizePresetName(string name)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalidChar, '_');
        }
        return name;
    }

    private void CheckRepository(IModelRepository repository)
    {
        if (repository.Settings.UseBsuLauncher) return;
        var state = _stateService.GetStateFor(repository);
        if (state != CalculatedRepositoryStateEnum.Ready && state != CalculatedRepositoryStateEnum.ReadyPartial) return;
        if (repository.GetMods().All(mod => mod.GetCurrentSelection() is ModSelectionDisabled)) return; // don't create an empty preset

        var presetName = SanitizePresetName(repository.Name);

        var dlcs = repository.GetServerInfo().CDLCs.Select(id => id.ToString()).ToList();
        var mods = GetModPaths(repository);

        var wasUpdatedTask = ArmaLauncher.UpdatePreset(presetName, mods, dlcs);
        wasUpdatedTask.ContinueInDispatcher(_dispatcher, wasUpdated =>
        {
            try
            {
                if (!wasUpdated()) return;

                // TODO: check if the launcher was open. can skip the re-start bit otherwise
                // TODO: don't really need to restart if it already existed / folders were watched.
                _eventManager.Publish(new NotificationEvent($"Arma Launcher Preset '{presetName}' was created. You might have to re-start the launcher."));
            }
            catch (Exception e)
            {
                _logger.Error(e);
                _eventManager.Publish(new ErrorEvent($"Failed to create preset '{repository.Name}'"));
            }
        });
    }

    private static List<string> GetModPaths(IModelRepository repository)
    {
        var result = new List<string>();
        foreach (var mod in repository.GetMods())
        {
            if (mod.GetCurrentSelection() is ModSelectionStorageMod storageMod)
                result.Add(storageMod.StorageMod.GetAbsolutePath());
        }

        return result;
    }
}
