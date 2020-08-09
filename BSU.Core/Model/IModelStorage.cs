using System;
using System.Collections.Generic;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal interface IModelStorage
    {
        List<IModelStorageMod> Mods { get; } // TODO: readonly
        IUpdateState PrepareDownload(IRepositoryMod repositoryMod, UpdateTarget target, string identifier);
        event Action<IModelStorageMod> ModAdded;
        bool CanWrite { get; }
        bool IsLoading { get; }
    }
}