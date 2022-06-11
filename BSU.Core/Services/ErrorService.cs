using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.Ioc;
using BSU.Core.Model;

namespace BSU.Core.Services;

internal interface IErrorService
{
    string? GetErrorForSelection(IModelRepositoryMod mod, IEnumerable<IModelRepositoryMod> allRepositoryMods);
}

internal class ErrorService : IErrorService
{
    private readonly IModActionService _modActionService;
    private readonly IConflictService _conflictService;

    internal ErrorService(IServiceProvider serviceProvider)
    {
        _modActionService = serviceProvider.Get<IModActionService>();
        _conflictService = serviceProvider.Get<IConflictService>();
    }
    
    public string? GetErrorForSelection(IModelRepositoryMod mod, IEnumerable<IModelRepositoryMod> allRepositoryMods)
    {
        var selection = mod.GetCurrentSelection();

        switch (selection)
        {
            case ModSelectionDisabled:
                return null;
            case ModSelectionDownload download when !download.IsNameValid(out var error):
            {
                return error;
            }
            case ModSelectionDownload selectStorage:
            {
                if (selectStorage.DownloadStorage.State == LoadingState.Loading) return null;
                var folderExists = selectStorage.DownloadStorage.HasMod(selectStorage.DownloadName);
                return folderExists ? "Name in use" : null;
            }
            case ModSelectionStorageMod selectMod when _modActionService.GetModAction(mod, selectMod.StorageMod) == ModActionEnum.AbortActiveAndUpdate:
                return "This mod is currently being updated";
            case ModSelectionStorageMod selectMod:
            {
                var conflicts = _conflictService.GetConflictsUsingMod(mod, selectMod.StorageMod, allRepositoryMods);
                if (!conflicts.Any())
                    return null;

                var conflictNames = conflicts.Select(c => $"{c}");
                return "In conflict with: " + string.Join(", ", conflictNames);
            }
            default:
                return null;
        }
    }
}
