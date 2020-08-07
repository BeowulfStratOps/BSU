using System;
using System.Collections.Generic;

namespace BSU.Core.Model
{
    internal interface IModelStorage
    {
        List<IModelStorageMod> Mods { get; } // TODO: readonly
        IUpdateState PrepareDownload(IModelRepositoryMod repositoryMod, string identifier);
        event Action<IModelStorageMod> ModAdded;
        bool CanWrite { get; }
        bool IsLoading { get; }
    }
}