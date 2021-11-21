using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
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
        private readonly Logger _logger;

        private readonly List<SyncWorkUnit> _allActions; // TODO: can be read only

        private RepoSync(List<SyncWorkUnit> actions, Logger logger)
        {
            _allActions = actions;
            _logger = logger;
        }

        public static async Task<RepoSync> BuildAsync(IRepositoryMod repository, StorageMod storage, CancellationToken cancellationToken, Guid guid)
        {
            var logger = LogHelper.GetLoggerWithIdentifier(typeof(RepoSync), guid.ToString());
            logger.Debug("Building sync actions {0} to {1}", storage, repository);

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

            logger.Debug("Download actions: {0}", allActions.OfType<DownloadAction>().Count());
            logger.Debug("Update actions: {0}", allActions.OfType<UpdateAction>().Count());
            logger.Debug("Delete actions: {0}", allActions.OfType<DeleteAction>().Count());

            return new RepoSync(allActions, logger);
        }

        public async Task<UpdateResult> UpdateAsync(CancellationToken cancellationToken, IProgress<FileSyncStats> progress)
        {
            var tasks = _allActions.Select(a =>
                ConcurrencyThrottle.Do(() => a.DoAsync(cancellationToken), cancellationToken));
            var whenAll = Task.WhenAll(tasks);

            try
            {
                await whenAll.WithUpdates(TimeSpan.FromMilliseconds(50), () => ProgressCallback(progress));
            }
            catch (IOException e) when ((e.HResult & 0xFFFF) == 32) // 32: sharing violation, aka open in arma
            {
                _logger.Error(e);
                return UpdateResult.FailedSharingViolation;
            }
            catch (Exception e)
            {
                _logger.Error(e);
                return UpdateResult.Failed;
            }

            _logger.Trace("Progress: None");
            progress?.Report(new FileSyncStats(FileSyncState.None));
            return UpdateResult.Success;
        }

        private void ProgressCallback(IProgress<FileSyncStats> progress)
        {
            ulong sumDone = 0;
            ulong sumTotal = 0;

            foreach (var syncWorkUnit in _allActions)
            {
                // not synchronized, but that's ok. we're just looking for a snapshot
                var stats = syncWorkUnit.GetStats();
                sumDone += stats.Done;
                sumTotal += stats.Total;
            }

            _logger.Trace("Progress: Updating");
            progress?.Report(new FileSyncStats(FileSyncState.Updating, sumTotal, sumDone));
        }
    }
}
