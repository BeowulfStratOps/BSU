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

        IUpdateState PrepareDownload(IRepositoryMod repositoryMod, UpdateTarget target, string identifier,
            Action<IModelStorageMod> createdCallback);
        bool CanWrite { get; }
        Guid Identifier { get; }
        string Name { get; }
        PersistedSelection AsStorageIdentifier();
        Task<bool> HasMod(string downloadIdentifier);
        event Action<IModelStorageMod> ModAdded;
        Task Load();
    }
}
