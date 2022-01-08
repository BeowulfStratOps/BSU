using System;
using System.Collections.Generic;
using BSU.Core.Launch;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal interface IModelRepository
    {
        List<IModelRepositoryMod> GetMods();
        Guid Identifier { get; }
        string Name { get; }
        LoadingState State { get; }
        PresetSettings Settings { get; set; }
        ServerInfo GetServerInfo();
        event Action<IModelRepository> StateChanged;
        GameLaunchHandle Launch();
    }
}
