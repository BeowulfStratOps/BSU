﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Model.Updating;
using BSU.Core.Sync;

namespace BSU.Core.Model
{
    internal interface IModelRepositoryMod
    {
        public void SetSelection(RepositoryModActionSelection selection);
        public Task<RepositoryModActionSelection> GetSelection(bool reset = false, CancellationToken cancellationToken = default);
        string DownloadIdentifier { get; set; }
        string Identifier { get; }
        IModelRepository ParentRepository { get; }
        Task<IModUpdate> StartUpdate(IProgress<FileSyncStats> progress, CancellationToken cancellationToken);
        Task<ModInfo> GetModInfo(CancellationToken cancellationToken);
        Task<MatchHash> GetMatchHash(CancellationToken cancellationToken);
        Task<VersionHash> GetVersionHash(CancellationToken cancellationToken);
        Task<List<IModelRepositoryMod>> GetConflictsUsingMod(IModelStorageMod mod, CancellationToken cancellationToken);
        Task<ModActionEnum> GetActionForMod(IModelStorageMod storageMod, CancellationToken cancellationToken);
        Task<List<(IModelStorageMod mod, ModActionEnum action)>> GetModActions(CancellationToken cancellationToken); // TODO: this method is kinda useless...
        Task<string> GetErrorForSelection(CancellationToken cancellationToken);
        RepositoryModActionSelection GetCurrentSelection();
    }
}
