using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal interface IModelRepositoryMod
    {
        UpdateTarget AsUpdateTarget { get; }
        public RepositoryModActionSelection Selection { get; set; }
        string DownloadIdentifier { get; set; }
        string Identifier { get; }
        bool IsLoaded { get; }
        event Action OnLoaded;
        event Action<IModelStorageMod> LocalModUpdated;
        event Action SelectionChanged;
        IUpdateState DoUpdate();
        void ProcessMod(IModelStorageMod storageMod);
        Task<string> GetDisplayName();
        void SignalAllStorageModsLoaded();
        MatchHash GetMatchHash();
        VersionHash GetVersionHash();
    }
}
