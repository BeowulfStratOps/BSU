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
        void DoUpdate(Action<Action<bool>> onPrepared);
    }
}
