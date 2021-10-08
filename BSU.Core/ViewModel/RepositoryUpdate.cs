using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Model;
using BSU.Core.Model.Updating;
using BSU.Core.Sync;
using NLog;

namespace BSU.Core.ViewModel
{

    internal class RepositoryUpdate : IRepositoryUpdate
    {
        private readonly List<(IModUpdate update, Progress<FileSyncStats> progress)> _updates;
        private readonly IProgress<FileSyncStats> _progress;

        private readonly Dictionary<IModUpdate, FileSyncStats> _lastProgress = new();
        private readonly object _progressLock = new();
        private readonly ILogger _logger;

        private bool _prepared, _updated;

        public async Task<StageStats> Prepare(CancellationToken cancellationToken)
        {
            if (_prepared) throw new InvalidOperationException();
            _prepared = true;

            cancellationToken.Register(() => ReportProgress(new FileSyncStats(FileSyncState.Stopping)));

            var tasks = _updates.Select(async s =>
            {
                var result = await s.update.Prepare(cancellationToken);
                return (result, s);
            }).ToList();

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // ignored
                // we handle those separately
            }

            var failedMods = tasks.Where(t => t.Result.result == UpdateResult.Failed).Select(t => t.Result.s.update.GetStorageMod()).ToList();
            var successCount = tasks.Count(t => t.Result.result == UpdateResult.Success);

            return new StageStats(successCount, failedMods, new List<IModelStorageMod>());
        }

        public async Task<StageStats> Update(CancellationToken cancellationToken)
        {
            if (_updated) throw new InvalidOperationException();
            _updated = true;

            cancellationToken.Register(() => ReportProgress(new FileSyncStats(FileSyncState.Stopping)));

            var tasks = _updates.Where(u => u.update.IsPrepared).Select(async s =>
            {
                var result = await s.update.Update(cancellationToken);
                return (result, s);
            }).ToList();
            var whenAll = Task.WhenAll(tasks);

            try
            {
                await whenAll.WithUpdates(TimeSpan.FromMilliseconds(50), () => ReportProgress(GetProgress()));
            }
            finally
            {
                ReportProgress(new FileSyncStats(FileSyncState.None));
            }

            var failedMods = tasks.Where(t => t.Result.result == UpdateResult.Failed).Select(t => t.Result.s.update.GetStorageMod()).ToList();
            var failedSvMods = tasks.Where(t => t.Result.result == UpdateResult.FailedSharingViolation).Select(t => t.Result.s.update.GetStorageMod()).ToList();
            var successCount = tasks.Count(t => t.Result.result == UpdateResult.Success);

            return new StageStats(successCount, failedMods, failedSvMods);
        }

        private FileSyncStats GetProgress()
        {
            lock (_progressLock)
            {
                if (_lastProgress.Values.Any(p => p.State == FileSyncState.Waiting))
                    return new FileSyncStats(FileSyncState.Waiting);
                if (_lastProgress.Values.Any(p => p.State == FileSyncState.Stopping))
                    return new FileSyncStats(FileSyncState.Stopping);
                if (_lastProgress.Values.Any(p => p.State == FileSyncState.Stopping))
                    return new FileSyncStats(FileSyncState.None);
                long sumDownloadTotal = 0;
                long sumDownloadDone = 0;
                long sumUpdateTotal = 0;
                long sumUpdateDone = 0;
                foreach (var progressValue in _lastProgress.Values)
                {
                    sumDownloadTotal += progressValue.DownloadTotal;
                    sumDownloadDone += progressValue.DownloadDone;
                    sumUpdateTotal += progressValue.UpdateTotal;
                    sumUpdateDone += progressValue.UpdateDone;
                }

                return new FileSyncStats(FileSyncState.Updating, sumDownloadTotal, sumUpdateTotal, sumDownloadDone, sumUpdateDone);
            }
        }

        private void ReportProgress(FileSyncStats stats)
        {
            _logger.Trace($"Progress: {stats.State}");
            _progress?.Report(stats);
        }

        internal RepositoryUpdate(List<(IModUpdate update, Progress<FileSyncStats> progress)> updates, IProgress<FileSyncStats> progress)
        {
            _logger = LogHelper.GetLoggerWithIdentifier(this, Guid.NewGuid().ToString());
            _updates = updates;
            _progress = progress;
            foreach (var (update, modProgress) in updates)
            {
                _lastProgress.Add(update, new FileSyncStats(FileSyncState.Waiting));
                modProgress.ProgressChanged += (_, e) => ModProgressOnProgressChanged(e, update);
            }

            _logger.Trace("Progress Waiting");
            _progress?.Report(new FileSyncStats(FileSyncState.Waiting));
        }

        private void ModProgressOnProgressChanged(FileSyncStats progress, IModUpdate update)
        {
            lock (_progressLock)
            {
                _lastProgress[update] = progress;
            }
        }
    }

    internal class StageStats
    {
        public int SucceededCount { get; }
        public List<IModelStorageMod> Failed { get; }
        public List<IModelStorageMod> FailedSharingViolation { get; }

        public StageStats(int succeededCount, List<IModelStorageMod> failed, List<IModelStorageMod> failedSharingViolation)
        {
            SucceededCount = succeededCount;
            Failed = failed;
            FailedSharingViolation = failedSharingViolation;
        }
    }
}
