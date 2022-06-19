using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.Core.Events;
using BSU.Core.Ioc;
using BSU.Core.Launch;
using BSU.Core.Model;
using NLog;

namespace BSU.Core.Services;

internal class BiKeyCopyActor
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IRepositoryStateService _stateService;
    private readonly DirectoryInfo? _keyDirectory;
    private readonly IModel _model;

    public BiKeyCopyActor(IServiceProvider serviceProvider)
    {
        var eventManager = serviceProvider.Get<IEventManager>();
        _stateService = serviceProvider.Get<IRepositoryStateService>();
        _model = serviceProvider.Get<IModel>();

        var gamePath = ArmaData.GetGamePath();

        if (gamePath == null) return;

        _keyDirectory = new DirectoryInfo(Path.Combine(gamePath, "Keys"));

        if (!_keyDirectory.Exists)
        {
            _logger.Warn("Couldn't find Key directory");
            return;
        }

        eventManager.Subscribe<CalculatedStateChangedEvent>(evt => CheckRepository(evt.Repository));
    }

    private void CheckRepository(IModelRepository repository)
    {
        var state = _stateService.GetRepositoryState(repository, _model.GetRepositoryMods());
        if (state != CalculatedRepositoryStateEnum.Ready && state != CalculatedRepositoryStateEnum.ReadyPartial) return;

        // enumerate once, as we call the CheckRepository function a LOT, and in most cases we don't need to change anything
        var existingFiles = _keyDirectory!.EnumerateFiles("*.bikey").Select(fi => fi.Name).ToHashSet();

        foreach (var mod in repository.GetMods())
        {
            if (mod.GetCurrentSelection() is not ModSelectionStorageMod storageMod) continue;

            foreach (var (name, content) in storageMod.StorageMod.GetKeyFiles())
            {
                // we assume that key files never change
                if (existingFiles.Contains(name)) continue;

                var keyPath = Path.Combine(_keyDirectory!.FullName, name);
                File.WriteAllBytes(keyPath, content);
            }
        }
    }
}
