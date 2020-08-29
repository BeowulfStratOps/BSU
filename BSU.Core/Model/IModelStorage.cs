using System;
using System.Collections.Generic;
using BSU.Core.Persistence;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal interface IModelStorage
    {
        List<IModelStorageMod> Mods { get; } // TODO: readonly

        void PrepareDownload(IRepositoryMod repositoryMod, UpdateTarget target, string identifier,
            Action<Exception> setupError, Action<IUpdateState> callback);
        event Action<IModelStorageMod> ModAdded;
        bool CanWrite { get; }
        bool IsLoading { get; }
        PersistedSelection GetStorageIdentifier();
    }
}