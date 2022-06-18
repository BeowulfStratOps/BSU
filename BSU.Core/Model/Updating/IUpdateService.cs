using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Sync;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model.Updating;

internal interface IUpdateService
{
    Task<UpdateResult> UpdateAsync(IRepositoryMod repositoryMod, IStorageMod storageMod, CancellationToken cancellationToken, IProgress<FileSyncStats>? progress);
}

internal class UpdateService : IUpdateService
{
    private readonly IJobManager _jobManager;

    public UpdateService(IJobManager jobManager)
    {
        _jobManager = jobManager;
    }

    public async Task<UpdateResult> UpdateAsync(IRepositoryMod repositoryMod, IStorageMod storageMod,
        CancellationToken cancellationToken, IProgress<FileSyncStats>? progress)
    {
        var result = await _jobManager.Run(() => RepoSync.UpdateAsync(repositoryMod, storageMod, cancellationToken,
            progress), cancellationToken);

        return result;
    }
}
