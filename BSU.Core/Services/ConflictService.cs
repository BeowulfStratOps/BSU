using System;
using System.Collections.Generic;
using BSU.Core.Ioc;
using BSU.Core.Model;
using BSU.CoreCommon.Hashes;

namespace BSU.Core.Services;

internal interface IConflictService
{
    List<IModelRepositoryMod> GetConflictsUsingMod(IModelRepositoryMod repoMod, IModelStorageMod storageMod, IEnumerable<IModelRepositoryMod> allRepoMods);

    bool IsConflicting(IModelRepositoryMod origin, IModelRepositoryMod otherMod,
        IModelStorageMod selected);
}

internal class ConflictService : IConflictService
{
    private readonly IModActionService _modActionService;

    internal ConflictService(IServiceProvider serviceProvider)
    {
        _modActionService = serviceProvider.Get<IModActionService>();
    }
    
    public List<IModelRepositoryMod> GetConflictsUsingMod(IModelRepositoryMod repoMod, IModelStorageMod storageMod, IEnumerable<IModelRepositoryMod> allRepoMods)
    {
        var result = new List<IModelRepositoryMod>();

        foreach (var mod in allRepoMods)
        {
            if (mod == repoMod) continue;
            if (mod.GetCurrentSelection() is not ModSelectionStorageMod otherMod || otherMod.StorageMod != storageMod) continue;
            if (IsConflicting(repoMod, mod, storageMod))
                result.Add(mod);
        }

        return result;
    }

    public bool IsConflicting(IModelRepositoryMod origin, IModelRepositoryMod otherMod,
        IModelStorageMod selected)
    {
        if (origin.State == LoadingState.Loading || otherMod.State == LoadingState.Loading ||
            selected.GetState() == StorageModStateEnum.Loading) return false;

        if (HashHelper.CheckHash(HashType.Version, otherMod, origin) != false)
            return false; // that's fine, won't break anything

        if (HashHelper.CheckHash(HashType.Match, otherMod, selected) != true)
            return false; // unrelated mod or loading, we don't care

        var actionType = _modActionService.GetModAction(origin, selected);

        if (actionType == ModActionEnum.Use) return false; // not our problem. only show conflict when we're trying to change something

        return true;
    }
}
