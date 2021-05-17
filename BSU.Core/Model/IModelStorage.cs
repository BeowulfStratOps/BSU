using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal interface IModelStorage
    {
        Task<List<IModelStorageMod>> GetMods();

        IUpdateCreate PrepareDownload(IRepositoryMod repositoryMod, UpdateTarget target, string identifier,
            Action<IModelStorageMod> createdCallback, MatchHash matchHash, VersionHash versionHash);
        bool CanWrite { get; }
        Guid Identifier { get; }
        string Name { get; }
        PersistedSelection AsStorageIdentifier();
        Task<bool> HasMod(string downloadIdentifier);
        event Action<IModelStorageMod> ModAdded;
        Task Load();
    }
}
