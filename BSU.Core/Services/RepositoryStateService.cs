using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.Ioc;
using BSU.Core.Model;

namespace BSU.Core.Services;

internal interface IRepositoryStateService
{
    CalculatedRepositoryStateEnum GetRepositoryState(IModelRepository repo, IEnumerable<IModelRepositoryMod> allRepositoryMods);
}

internal class RepositoryStateService : IRepositoryStateService
{
    private readonly IModActionService _modActionService;
    private readonly IErrorService _errorService;

    internal RepositoryStateService(IServiceProvider serviceProvider)
    {
        _modActionService = serviceProvider.Get<IModActionService>();
        _errorService = serviceProvider.Get<IErrorService>();
    }
    
    public CalculatedRepositoryStateEnum GetRepositoryState(IModelRepository repo, IEnumerable<IModelRepositoryMod> allRepositoryMods)
    {
        /*
        NeedsSync, // auto selected previously used, other auto selection worked without any conflicts, no internal conflicts.
        Ready, // All use
        RequiresUserIntervention, // Else
        Syncing, // All are ready or being worked on
        Loading
        */

        if (repo.State == LoadingState.Loading) return CalculatedRepositoryStateEnum.Loading;
        if (repo.State == LoadingState.Error) return CalculatedRepositoryStateEnum.Error;

        var mods = repo.GetMods();
        var allMods = allRepositoryMods.ToList();

        var infos = new List<(ModSelection? selection, ModActionEnum? action)>();

        foreach (var mod in mods)
        {
            var selection = mod.GetCurrentSelection();
            var action = selection is not ModSelectionStorageMod actionStorageMod
                ? null
                : (ModActionEnum?)_modActionService.GetModAction(mod, actionStorageMod.StorageMod);

            var error = _errorService.GetErrorForSelection(mod, allMods);
            if  (error != null)
                return CalculatedRepositoryStateEnum.RequiresUserIntervention;

            if (action == ModActionEnum.Loading || selection is ModSelectionLoading)
                return CalculatedRepositoryStateEnum.Loading;

            infos.Add((selection, action));
        }

        if (infos.All(mod =>
                mod.selection is ModSelectionStorageMod && mod.action == ModActionEnum.Use))
        {
            return CalculatedRepositoryStateEnum.Ready;
        }

        if (infos.All(mod => mod.selection is ModSelectionDisabled))
        {
            return CalculatedRepositoryStateEnum.RequiresUserIntervention;
        }

        if (infos.All(mod => mod.selection is ModSelectionDisabled || mod.action == ModActionEnum.Use))
        {
            return CalculatedRepositoryStateEnum.ReadyPartial;
        }

        if (infos.Any(mod => mod.selection is ModSelectionNone || mod.action == ModActionEnum.AbortActiveAndUpdate))
            return CalculatedRepositoryStateEnum.RequiresUserIntervention;

        if (infos.Any(mod => mod.action == ModActionEnum.Await))
            return CalculatedRepositoryStateEnum.Syncing;

        return CalculatedRepositoryStateEnum.NeedsSync;
    }
}
