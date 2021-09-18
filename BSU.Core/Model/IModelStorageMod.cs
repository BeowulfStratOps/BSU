using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.Core.Sync;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal interface IModelStorageMod
    {
        event Action StateChanged;
        Task<IModUpdate> PrepareUpdate(IRepositoryMod repositoryMod, string targetDisplayName, MatchHash targetMatch, VersionHash targetVersion, IProgress<FileSyncStats> progress);
        Task Abort();
        PersistedSelection GetStorageModIdentifiers();
        bool CanWrite { get; }
        string Identifier { get; }
        Task<VersionHash> GetVersionHash(CancellationToken cancellationToken);
        Task<MatchHash> GetMatchHash(CancellationToken cancellationToken);
        StorageModStateEnum GetState();
    }
}
