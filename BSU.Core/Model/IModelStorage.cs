using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BSU.Core.Persistence;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal interface IModelStorage
    {
        Task<List<IModelStorageMod>> GetMods();

        Task<IUpdateState> PrepareDownload(IRepositoryMod repositoryMod, UpdateTarget target, string identifier);
        event Action<IModelStorageMod> ModAdded;
        bool CanWrite { get; }
        Guid Identifier { get; }
        string Name { get; }
        PersistedSelection GetStorageIdentifier();
        Task<bool> HasMod(string downloadIdentifier);
    }
}