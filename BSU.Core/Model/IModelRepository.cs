using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model.Updating;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal interface IModelRepository
    {
        Task<List<IModelRepositoryMod>> GetMods();
        Guid Identifier { get; }
        string Name { get; }
        Task<CalculatedRepositoryState> GetState(CancellationToken cancellationToken);
        Task<ServerInfo> GetServerInfo(CancellationToken cancellationToken);
    }
}
