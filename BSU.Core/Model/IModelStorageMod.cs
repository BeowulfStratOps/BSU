using System;
using System.Collections.Generic;
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
        event Action<IModelStorageMod> StateChanged;
        Task<UpdateResult> Update(IRepositoryMod repositoryMod, MatchHash targetMatch, VersionHash targetVersion,
            IProgress<FileSyncStats>? progress, CancellationToken cancellationToken);
        void Abort();
        PersistedSelection GetStorageModIdentifiers();
        bool CanWrite { get; }
        string Identifier { get; }
        IModelStorage ParentStorage { get; }
        bool IsDeleted { get; }
        VersionHash GetVersionHash();
        MatchHash GetMatchHash();
        StorageModStateEnum GetState();
        string GetTitle();
        void Delete(bool removeData);
        string GetAbsolutePath();
    }
}
