using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BSU.Core.Persistence;

namespace BSU.Core.Model
{
    internal interface IModelStorage
    {
        List<IModelStorageMod> GetMods();

        Task<IModelStorageMod> CreateMod(string identifier, UpdateTarget updateTarget);
        bool CanWrite { get; }
        Guid Identifier { get; }
        string Name { get; }
        bool IsDeleted { get; }
        LoadingState State { get; }
        PersistedSelection AsStorageIdentifier();
        bool HasMod(string downloadIdentifier);
        string GetLocation();
        bool IsAvailable();
        void Delete(bool removeMods);
        event Action<IModelStorage> StateChanged;
    }
}
