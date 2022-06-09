using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.Core.Sync;
using BSU.CoreCommon;
using BSU.CoreCommon.Hashes;
using BSU.Hashes;

namespace BSU.Core.Model
{
    internal interface IModelStorageMod : IHashCollection
    {
        event Action StateChanged;
        Task<UpdateResult> Update(IRepositoryMod repositoryMod, UpdateTarget target,
            IProgress<FileSyncStats>? progress, CancellationToken cancellationToken);
        void Abort();
        PersistedSelection GetStorageModIdentifiers();
        bool CanWrite { get; }
        string Identifier { get; }
        IModelStorage ParentStorage { get; }
        bool IsDeleted { get; }
        StorageModStateEnum GetState();
        void Delete(bool removeData);
        string GetAbsolutePath();
        Task<Dictionary<string, byte[]>> GetKeyFiles(CancellationToken token);
        string GetTitle();
    }
}
