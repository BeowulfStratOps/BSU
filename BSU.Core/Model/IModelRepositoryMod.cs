using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal interface IModelRepositoryMod
    {
        UpdateTarget AsUpdateTarget { get; }
        public RepositoryModActionSelection Selection { get; set; }
        Dictionary<IModelStorageMod, ModAction> Actions { get; }
        IRepositoryMod Implementation { get; }
        string DownloadIdentifier { get; set; }
        string Identifier { get; }
        Task<RepositoryModState> GetState();
        void ChangeAction(IModelStorageMod target, ModActionEnum? newAction);
        event Action<IModelStorageMod> ActionAdded;
        event Action SelectionChanged;
        event Action DownloadIdentifierChanged;
        Task DoUpdate();
        IUpdateState CurrentUpdate { get; }
        event Action OnUpdateChange;
        Task ProcessMods(List<IModelStorage> mods);
        Task<string> GetDisplayName();
    }
}
