using System;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Persistence;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal interface IModelStorageMod
    {
        event Action StateChanged;
        IUpdateState PrepareUpdate(IRepositoryMod repositoryMod, UpdateTarget target);
        void Abort();
        PersistedSelection GetStorageModIdentifiers();
        bool CanWrite { get; }
        string Identifier { get; }
        Task<VersionHash> GetVersionHash();
        Task<MatchHash> GetMatchHash();
        StorageModStateEnum GetState();
    }
}