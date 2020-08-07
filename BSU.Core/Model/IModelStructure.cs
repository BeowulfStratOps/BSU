using System.Collections.Generic;

namespace BSU.Core.Model
{
    internal interface IModelStructure
    {
        Storage GetWritableStorage();
        IEnumerable<Storage> GetStorages();
        IEnumerable<Repository> GetRepositories();
        IEnumerable<IModelStorageMod> GetAllStorageMods();
        IEnumerable<IModelRepositoryMod> GetAllRepositoryMods();
    }
}