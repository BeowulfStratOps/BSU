﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace BSU.Core.Model
{
    internal interface IModelStructure
    {
        IEnumerable<IModelStorage> GetStorages();
        IEnumerable<IModelRepository> GetRepositories();
        Task<IEnumerable<IModelStorageMod>> GetAllStorageMods();
        Task<IEnumerable<IModelRepositoryMod>> GetAllRepositoryMods();
    }
}