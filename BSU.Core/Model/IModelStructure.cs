using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BSU.Core.Model
{
    internal interface IModelStructure
    {
        // TODO: abort all things when one of those changes
        IEnumerable<IModelStorage> GetStorages();
        IEnumerable<IModelRepository> GetRepositories();
        Task<IEnumerable<IModelStorageMod>> GetStorageMods();
        Task<IEnumerable<IModelRepositoryMod>> GetRepositoryMods();
    }
}
