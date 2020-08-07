using System.Collections.Generic;

namespace BSU.Core.Model
{
    internal interface IModelStructure
    {
        IEnumerable<IModelStorage> GetStorages();
        IEnumerable<IModelRepository> GetRepositories();
        IEnumerable<IModelStorageMod> GetAllStorageMods();
        IEnumerable<IModelRepositoryMod> GetAllRepositoryMods();
    }
}