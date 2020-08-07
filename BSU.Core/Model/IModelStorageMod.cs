using System;
using BSU.Core.Persistence;

namespace BSU.Core.Model
{
    internal interface IModelStorageMod
    {
        void RequireHash();
        event Action StateChanged;
        IUpdateState PrepareUpdate(RepositoryMod repositoryMod, Action rollback = null);
        StorageModState GetState();
        void Abort();
        StorageModIdentifiers GetStorageModIdentifiers();
        bool CanWrite { get; }
    }
}