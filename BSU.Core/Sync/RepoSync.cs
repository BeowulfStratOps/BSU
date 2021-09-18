using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.Core.Model.Updating;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Sync
{
    /// <summary>
    /// Job for updating/downloading a storage mod from a repository.
    /// </summary>
    internal class RepoSync
    {
        private static readonly Logger Logger = EntityLogger.GetLogger();

        private readonly List<SyncWorkUnit> _allActions; // TODO: can be read only

        private RepoSync(List<SyncWorkUnit> actions)
        {
            _allActions = actions;
        }

        public static async Task<RepoSync> BuildAsync(IRepositoryMod repository, StorageMod storage, CancellationToken cancellationToken)
        {
            Logger.Debug("Building sync actions {0} to {1}", storage, repository);

            var allActions = new List<SyncWorkUnit>();
            var repositoryList = await repository.GetFileList(cancellationToken);
            var storageList = await storage.Implementation.GetFileList(cancellationToken);
            var storageListCopy = new List<string>(storageList);
            foreach (var repoFile in repositoryList)
            {
                if (storageList.Contains(repoFile))
                {
                    var repoFileHash = await repository.GetFileHash(repoFile, cancellationToken);
                    var storageFileHash = await storage.Implementation.GetFileHash(repoFile, cancellationToken);
                    if (!repoFileHash.Equals(storageFileHash))
                    {
                        var fileSize = await repository.GetFileSize(repoFile, cancellationToken);
                        allActions.Add(new UpdateAction(repository, storage, repoFile, fileSize));
                    }

                    storageListCopy.Remove(repoFile);
                }
                else
                {
                    var fileSize = await repository.GetFileSize(repoFile, cancellationToken);
                    allActions.Add(new DownloadAction(repository, storage, repoFile, fileSize));
                }
            }

            foreach (var storageModFile in storageListCopy)
            {
                allActions.Add(new DeleteAction(storage, storageModFile));
            }

            Logger.Debug("Download actions: {0}", allActions.OfType<DownloadAction>().Count());
            Logger.Debug("Update actions: {0}", allActions.OfType<UpdateAction>().Count());
            Logger.Debug("Delete actions: {0}", allActions.OfType<DeleteAction>().Count());

            return new RepoSync(allActions);
        }

        public async Task UpdateAsync(CancellationToken cancellationToken, IProgress<FileSyncStats> progress)
        {
            // TODO: limit parallel disk/network usage
            var tasks = _allActions.Select(a => a.DoAsync(cancellationToken));
            var whenAll = Task.WhenAll(tasks);
            while (true)
            {
                await Task.WhenAny(whenAll, Task.Delay(50));

                long sumDownloadDone = 0;
                long sumDownloadTotal = 0;
                long sumUpdateDone = 0;
                long sumUpdateTotal = 0;

                foreach (var syncWorkUnit in _allActions)
                {
                    var stats = syncWorkUnit.GetStats(); // not synchronized, but that's ok. we're just looking for a snapshot
                    sumDownloadDone += stats.DownloadDone;
                    sumDownloadTotal += stats.DownloadTotal;
                    sumUpdateDone += stats.UpdateDone;
                    sumUpdateTotal += stats.UpdateTotal;

                    progress?.Report(new FileSyncStats(FileSyncState.Updating, sumDownloadTotal, sumUpdateTotal,
                        sumDownloadDone, sumUpdateDone));
                }

                if (whenAll.IsCompleted) break;
            }

            progress?.Report(new FileSyncStats(FileSyncState.None, 0, 0, 0, 0));
        }
    }
}
