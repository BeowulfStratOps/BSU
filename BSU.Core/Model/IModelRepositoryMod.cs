using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.Core.Sync;

namespace BSU.Core.Model
{
    internal interface IModelRepositoryMod
    {
        public void SetSelection(RepositoryModActionSelection selection);
        string DownloadIdentifier { get; set; }
        string Identifier { get; }
        IModelRepository ParentRepository { get; }
        LoadingState State { get; }
        Task<IModUpdate> StartUpdate(IProgress<FileSyncStats> progress, CancellationToken cancellationToken);
        ModInfo GetModInfo();
        MatchHash GetMatchHash();
        VersionHash GetVersionHash();
        RepositoryModActionSelection GetCurrentSelection();
        event Action<IModelRepositoryMod> StateChanged;
        PersistedSelection GetPreviousSelection();
        event Action<IModelRepositoryMod> SelectionChanged;
        event Action<IModelRepositoryMod> DownloadIdentifierChanged;
    }
}
