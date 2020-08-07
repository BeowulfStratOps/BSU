using System;
using System.Collections.Generic;

namespace BSU.Core.Model
{
    internal interface IModelRepositoryMod
    {
        UpdateTarget AsUpdateTarget { get; }
        IModelStorageMod SelectedStorageMod { get; set; }
        Storage SelectedDownloadStorage { get; set; }
        Dictionary<IModelStorageMod, ModAction> Actions { get; }
        bool AllModsLoaded { set; }
        event Action StateChanged;
        RepositoryModState GetState();
        void ChangeAction(IModelStorageMod target, ModActionEnum? newAction);
        event Action<IModelStorageMod> ActionAdded;
        event Action SelectionChanged;
    }
}