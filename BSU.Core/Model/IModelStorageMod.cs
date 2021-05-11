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
        IUpdateState PrepareUpdate(IRepositoryMod repositoryMod, UpdateTarget target, MatchHash targetMatch, VersionHash targetVersion);
        void Abort();
        PersistedSelection GetStorageModIdentifiers();
        bool CanWrite { get; }
        string Identifier { get; }
        VersionHash GetVersionHash();
        MatchHash GetMatchHash();
        StorageModStateEnum GetState();
        void RequireMatchHash();
        void RequireVersionHash();
    }
}
