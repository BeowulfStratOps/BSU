using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Model.Updating;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal interface IModelRepositoryMod
    {
        public void SetSelection(RepositoryModActionSelection selection);
        public Task<RepositoryModActionSelection> GetSelection(CancellationToken cancellationToken);
        string DownloadIdentifier { get; set; }
        string Identifier { get; }
        Task<IUpdateCreated> StartUpdate(CancellationToken cancellationToken);
        Task<string> GetDisplayName(CancellationToken cancellationToken);
        Task<MatchHash> GetMatchHash(CancellationToken cancellationToken);
        Task<VersionHash> GetVersionHash(CancellationToken cancellationToken);
    }
}
