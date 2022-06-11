using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.Model;

namespace BSU.Core.Services;

internal interface IStorageService
{
    string GetAvailableDownloadIdentifier(IModelStorage storage, string baseIdentifier);
    IEnumerable<IModelRepositoryMod> GetUsedBy(IModelStorageMod storageMod, IEnumerable<IModelRepositoryMod> allRepositoryMods);
}

internal class StorageService : IStorageService
{
    public string GetAvailableDownloadIdentifier(IModelStorage storage, string baseIdentifier)
    {
        bool Exists(string name)
        {
            return storage.GetMods().Any(
                m => string.Equals(m.Identifier, name, StringComparison.InvariantCultureIgnoreCase));
        }

        if (!Exists(baseIdentifier))
            return baseIdentifier;
        var i = 1;
        while (true)
        {
            var name = $"{baseIdentifier}_{i}";
            if (!Exists(name))
                return name;
            i++;
        }
    }
    
    public IEnumerable<IModelRepositoryMod> GetUsedBy(IModelStorageMod storageMod, IEnumerable<IModelRepositoryMod> allRepositoryMods)
    {
        var result = new List<IModelRepositoryMod>();
        foreach (var repositoryMod in allRepositoryMods)
        {
            var selection = repositoryMod.GetCurrentSelection();
            if (selection is ModSelectionStorageMod mod && mod.StorageMod == storageMod)
                result.Add(repositoryMod);
        }

        return result;
    }
}
