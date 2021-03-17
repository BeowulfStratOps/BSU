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
        Dictionary<IModelStorageMod, ModAction> LocalMods { get; }
        string DownloadIdentifier { get; set; }
        string Identifier { get; }
        event Action<IModelStorageMod> LocalModAdded;
        event Action SelectionChanged;
        IUpdateState DoUpdate();
        Task ProcessMods();
        Task<string> GetDisplayName();
    }
}
