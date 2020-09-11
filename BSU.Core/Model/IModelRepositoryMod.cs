﻿using System;
using System.Collections.Generic;
using BSU.Core.Model.Utility;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal interface IModelRepositoryMod
    {
        UpdateTarget AsUpdateTarget { get; }
        public RepositoryModActionSelection Selection { get; set; }
        Dictionary<IModelStorageMod, ModAction> Actions { get; }
        bool AllModsLoaded { set; }
        IRepositoryMod Implementation { get; }
        string DownloadIdentifier { get; set; }
        event Action StateChanged;
        RepositoryModState GetState();
        void ChangeAction(IModelStorageMod target, ModActionEnum? newAction);
        event Action<IModelStorageMod> ActionAdded;
        event Action SelectionChanged;
        event Action DownloadIdentifierChanged;
        ModUpdateInfo DoUpdate();
    }
}
