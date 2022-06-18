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
        private readonly List<SyncWorkUnit> _allActions;

        private RepoSync(List<SyncWorkUnit> actions, Logger logger)
        {
            _allActions = actions;
            _logger = logger;
        }

        private static async Task<RepoSync> BuildAsync(IRepositoryMod repository, IStorageMod storage, Logger logger,
            CancellationToken cancellationToken)
        {
            logger.Debug($"Building sync actions {storage} to {repository}");

            var allActions = new List<SyncWorkUnit>();
            var repositoryList = await repository.GetFileList(cancellationToken);
            var storageList = await storage.GetFileList(cancellationToken);
            var storageListCopy = new List<string>(storageList);
            foreach (var repoFile in repositoryList)
            {
                if (storageList.Contains(repoFile))
                {
                    var repoFileHash = await repository.GetFileHash(repoFile, cancellationToken);
                    var storageFileHash = await storage.GetFileHash(repoFile, cancellationToken);
                    if (!repoFileHash.Equals(storageFileHash))
                    {
                        var fileSize = await repository.GetFileSize(repoFile, cancellationToken);
                        allActions.Add(new UpdateAction(repository, storage, repoFile, fileSize));
                        storageListCopy.Remove(repoFile + ".part");
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

            logger.Debug($"Download actions: {allActions.OfType<DownloadAction>().Count()}");
            logger.Debug($"Update actions: {allActions.OfType<UpdateAction>().Count()}");
            logger.Debug($"Delete actions: {allActions.OfType<DeleteAction>().Count()}");

            return new RepoSync(allActions, logger);
        }

        private async Task UpdateAsync(CancellationToken cancellationToken, IProgress<FileSyncStats>? progress)
        {
            var task = ConcurrencyThrottle.Do(_allActions, a => a.DoAsync(cancellationToken), cancellationToken);

            await task.WithUpdates(TimeSpan.FromMilliseconds(50), () => ProgressCallback(progress));
        }

        public static async Task<UpdateResult> UpdateAsync(IRepositoryMod repository, IStorageMod storageMod, CancellationToken cancellationToken, IProgress<FileSyncStats>? progress)
        {
            progress?.Report(new FileSyncStats(FileSyncState.Waiting));

            cancellationToken.Register(() => progress?.Report(new FileSyncStats(FileSyncState.Stopping)));
            
            var logger = LogHelper.GetLoggerWithIdentifier(typeof(RepoSync), Guid.NewGuid().ToString());
            try
            {
                // TODO: add progress stages
                var repoSync = await BuildAsync(repository, storageMod, logger, cancellationToken);
                await repoSync.UpdateAsync(cancellationToken, progress);
            }
            catch (IOException e) when ((e.HResult & 0xFFFF) == 32) // 32: sharing violation, aka open in arma
            {
                logger.Error(e);
                return UpdateResult.FailedSharingViolation;
            }
            catch (Exception e)
            {
                logger.Error(e);
                return UpdateResult.Failed;
            }
            finally
            {
                progress?.Report(new FileSyncStats(FileSyncState.None));
            }

            logger.Trace("Progress: None");
            return UpdateResult.Success;
        }

        private void ProgressCallback(IProgress<FileSyncStats>? progress)
        {
            ulong sumDone = 0;
            ulong sumTotal = 0;

            foreach (var syncWorkUnit in _allActions)
            {
                // not synchronized, but that's ok. we're just looking for a snapshot
                var (_, total, done) = syncWorkUnit.GetStats();
                sumDone += done;
                sumTotal += total;
            }

            _logger.Trace("Progress: Updating");
            progress?.Report(new FileSyncStats(FileSyncState.Updating, sumTotal, sumDone));
        }
    }
}
