using System;
using System.Collections.Generic;

namespace BSU.Core.Model
{
    internal interface IModelRepository
    {
        List<IModelRepositoryMod> Mods { get; }
        CalculatedRepositoryState CalculatedState { get; }
        event Action CalculatedStateChanged;
        event Action<IModelRepositoryMod> ModAdded;
        bool IsLoading { get; }
        Guid Identifier { get; }
        string Name { get; }

        void DoUpdate(RepositoryUpdate.SetUpDelegate setup, RepositoryUpdate.PreparedDelegate prepared,
            RepositoryUpdate.FinishedDelegate finished);
    }
}
