using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.Ioc;
using BSU.Core.Model;
using NLog;

namespace BSU.Core.Services;

internal interface IAutoSelectionService
{
    IModelStorageMod? AutoSelect(IModelRepositoryMod repoMod,
        IEnumerable<IModelStorageMod> storageMods, List<IModelRepositoryMod> allRepoMods, SteamUsage steamUsage);

    ModSelection? GetSelection(IModel model, IModelRepositoryMod mod, SteamUsage steamUsage);
    bool HasInvalidSelection(IModelRepositoryMod mod, bool useSteam);

    ModSelection? GetAutoSelection(IModel model, IModelRepositoryMod mod,
        SteamUsage steamUsage = SteamUsage.UseSteamButPreferLocal, bool allowSwitching = false);
    
    public enum SteamUsage
    {
        DontUseSteam,
        UseSteamButPreferLocal,
        UseSteamAndPreferIt
    }
}

internal class AutoSelectionService : IAutoSelectionService
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IConflictService _conflictService;
    private readonly IModActionService _modActionService;
    private readonly IStorageService _storageService;

    internal AutoSelectionService(IServiceProvider serviceProvider)
    {
        _conflictService = serviceProvider.Get<IConflictService>();
        _modActionService = serviceProvider.Get<IModActionService>();
        _storageService = serviceProvider.Get<IStorageService>();
    }

    public IModelStorageMod? AutoSelect(IModelRepositoryMod repoMod,
        IEnumerable<IModelStorageMod> storageMods, List<IModelRepositoryMod> allRepoMods, IAutoSelectionService.SteamUsage steamUsage)
    {
        var byActionType = new Dictionary<ModActionEnum, List<IModelStorageMod>>();

        foreach (var storageMod in storageMods)
        {
            if (_conflictService.GetConflictsUsingMod(repoMod, storageMod, allRepoMods).Any())
                continue;
            var action = _modActionService.GetModAction(repoMod, storageMod);
            byActionType.AddInBin(action, storageMod);
        }

        // Order of precedence
        var precedence = new[]
            {ModActionEnum.Use, ModActionEnum.Await, ModActionEnum.ContinueUpdate, ModActionEnum.Update};

        foreach (var actionType in precedence)
        {
            if (!byActionType.TryGetValue(actionType, out var candidates))
                continue;

            var foundSteam = candidates.FirstOrDefault(mod => !mod.CanWrite);
            var foundNonSteam = candidates.FirstOrDefault(mod => mod.CanWrite);

            if (steamUsage == IAutoSelectionService.SteamUsage.DontUseSteam)
            {
                if (foundNonSteam != null)
                    return foundNonSteam;
                continue;
            }

            if (steamUsage == IAutoSelectionService.SteamUsage.UseSteamAndPreferIt)
            {
                var preferred = foundSteam ?? foundNonSteam;
                if (preferred != null)
                    return preferred;
                continue;
            }

            if (steamUsage == IAutoSelectionService.SteamUsage.UseSteamButPreferLocal)
            {
                var preferred = foundNonSteam ?? foundSteam;
                if (preferred != null)
                    return preferred;
                continue;
            }
        }

        return null;
    }

    public ModSelection? GetSelection(IModel model, IModelRepositoryMod mod, IAutoSelectionService.SteamUsage steamUsage)
    {
        if (mod.State == LoadingState.Error) return new ModSelectionDisabled();
        
        _logger.Trace($"Checking auto-selection for mod {mod.Identifier}");

        if (model.GetStorages().Any(s => s.State == LoadingState.Loading)) return new ModSelectionLoading();

        var storageMods = model.GetStorageMods().ToList();

        var previouslySelectedMod =
            storageMods.SingleOrDefault(m => m.GetStorageModIdentifiers().Equals(mod.GetPreviousSelection()));

        if (previouslySelectedMod != null)
            return new ModSelectionStorageMod(previouslySelectedMod);

        // TODO: check previously selected storage for download?

        // wait for everything to load.
        // TODO: only wait for things we really need here
        if (model.GetStorageMods().Any(s => s.GetState() == StorageModStateEnum.Loading)) return new ModSelectionLoading();
        if (model.GetRepositories().Any(s => s.State == LoadingState.Loading)) return new ModSelectionLoading();
        if (model.GetRepositoryMods().Any(s => s.State == LoadingState.Loading)) return new ModSelectionLoading();

        var selectedMod = AutoSelect(mod, storageMods, model.GetRepositoryMods(), steamUsage);

        if (selectedMod != null)
            return new ModSelectionStorageMod(selectedMod);

        var storage = model.GetStorages().FirstOrDefault(s => s.CanWrite && s.IsAvailable());
        if (storage != null)
        {
            var downloadName = _storageService.GetAvailableDownloadIdentifier(storage, mod.Identifier);
            return new ModSelectionDownload(storage, downloadName);
        }

        return new ModSelectionNone();
    }

    public bool HasInvalidSelection(IModelRepositoryMod mod, bool useSteam)
    {
        var currentSelection = mod.GetCurrentSelection();

        if (mod.State == LoadingState.Error && currentSelection is not ModSelectionDisabled) return true;
        
        return currentSelection switch
        {
            ModSelectionNone => true,
            ModSelectionLoading => true,
            ModSelectionDownload download when download.DownloadStorage.IsDeleted => true,
            ModSelectionStorageMod storageMod when storageMod.StorageMod.IsDeleted ||
                                                   (!storageMod.StorageMod.CanWrite && !useSteam) => true,
            _ => false
        };
    }

    public ModSelection? GetAutoSelection(IModel model, IModelRepositoryMod mod, IAutoSelectionService.SteamUsage steamUsage = IAutoSelectionService.SteamUsage.UseSteamButPreferLocal, bool allowSwitching = false)
    {
        var shouldDoSelection = allowSwitching || HasInvalidSelection(mod, steamUsage != IAutoSelectionService.SteamUsage.DontUseSteam);
        return shouldDoSelection ? GetSelection(model, mod, steamUsage) : null;
    }
}
