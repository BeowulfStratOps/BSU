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

internal class PresetGeneratorActor
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IRepositoryStateService _stateService;
    private readonly IEventManager _eventManager;
    private readonly IDispatcher _dispatcher;
    private readonly IServiceProvider _serviceProvider;

    public PresetGeneratorActor(IServiceProvider serviceProvider)
    {
        var eventManager = serviceProvider.Get<IEventManager>();
        _stateService = serviceProvider.Get<IRepositoryStateService>();
        _eventManager = serviceProvider.Get<IEventManager>();
        _dispatcher = serviceProvider.Get<IDispatcher>();
        _serviceProvider = serviceProvider;
        eventManager.Subscribe<CalculatedStateChangedEvent>(evt =>
            CheckRepository(evt.Repository, _serviceProvider.Get<IModel>()));
        eventManager.Subscribe<SettingsChangedEvent>(_ =>
        {
            var model = _serviceProvider.Get<IModel>();
            foreach (var repository in model.GetRepositories())
            {
                CheckRepository(repository, model);
            }
        });
    }

    private string SanitizePresetName(string name)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalidChar, '_');
        }
        return name;
    }

    private void CheckRepository(IModelRepository repository, IModel model)
    {
        var settings = _serviceProvider.Get<IModel>().GetSettings();
        if (settings.UseBsuLauncher) return;

        var state = _stateService.GetRepositoryState(repository, model.GetRepositoryMods());
        if (state != CalculatedRepositoryStateEnum.Ready && state != CalculatedRepositoryStateEnum.ReadyPartial) return;
        if (repository.GetMods().All(mod => mod.GetCurrentSelection() is ModSelectionDisabled)) return; // don't create an empty preset

        var presetName = SanitizePresetName(repository.Name);

        var dlcs = repository.GetServerInfo().Cdlcs.Select(id => id.ToString()).ToList();
        var mods = GetModPaths(repository);
        var steamMods = GetSteamMods(repository);

        var wasUpdatedTask = ArmaLauncher.UpdatePreset(presetName, mods, steamMods, dlcs);
        wasUpdatedTask.ContinueInDispatcher(_dispatcher, wasUpdated =>
        {
            try
            {
                if (!wasUpdated()) return;

                // TODO: check if the launcher was open. can skip the re-start bit otherwise
                // TODO: don't really need to restart if it already existed / folders were watched.
                _eventManager.Publish(new NotificationEvent($"Arma Launcher Preset was created. You might have to re-start the launcher."));
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
            if (mod.GetCurrentSelection() is ModSelectionStorageMod storageMod && storageMod.StorageMod.CanWrite)
                result.Add(storageMod.StorageMod.GetAbsolutePath());
        }

        return result;
    }

    private static List<string> GetSteamMods(IModelRepository repository)
    {
        var result = new List<string>();
        foreach (var mod in repository.GetMods())
        {
            if (mod.GetCurrentSelection() is ModSelectionStorageMod storageMod && !storageMod.StorageMod.CanWrite)
                result.Add(storageMod.StorageMod.Identifier);
        }

        return result;
    }
}
