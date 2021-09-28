using System;
using System.Collections.Generic;
using System.Threading;
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

        Task<IModelStorageMod> CreateMod(string identifier, UpdateTarget updateTarget);
        bool CanWrite { get; }
        Guid Identifier { get; }
        string Name { get; }
        PersistedSelection AsStorageIdentifier();
        Task<bool> HasMod(string downloadIdentifier);
        string GetLocation();
    }
}
