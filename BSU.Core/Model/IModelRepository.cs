using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BSU.Core.Model
{
    internal interface IModelRepository
    {
        Task<List<IModelRepositoryMod>> GetMods();
        CalculatedRepositoryState CalculatedState { get; }
        event Action<CalculatedRepositoryState> CalculatedStateChanged;
        event Action<IModelRepositoryMod> ModAdded;
        Guid Identifier { get; }
        string Name { get; }

        RepositoryUpdate DoUpdate();
        Task ProcessMods();
    }
}
