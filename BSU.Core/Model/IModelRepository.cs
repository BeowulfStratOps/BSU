using System;
using System.Collections.Generic;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal interface IModelRepository
    {
        List<IModelRepositoryMod> GetMods();
        Guid Identifier { get; }
        string Name { get; }
        LoadingState State { get; }
        ServerInfo GetServerInfo();
        event Action<IModelRepository> StateChanged;
    }
}
