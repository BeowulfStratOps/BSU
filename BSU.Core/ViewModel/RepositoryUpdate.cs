using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Model;
using BSU.Core.Model.Updating;
using BSU.Core.Sync;
using NLog;

namespace BSU.Core.ViewModel
{

    internal class RepositoryUpdate
    {
        private readonly List<ModUpdate> _updates;
        private readonly IProgress<FileSyncStats> _progress;

        private readonly Dictionary<IModelStorageMod, FileSyncStats> _lastProgress = new();
        private readonly Dictionary<IModelStorageMod, ulong> _totalSizes = new ();
        private readonly object _progressLock = new();
        private readonly ILogger _logger;

        public async Task<StageStats> Update()
        {
            var tasks = _updates.Select(async s =>
            {
                var result = await s.Update;
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

            var failedMods = tasks.Where(t => t.Result.result == UpdateResult.Failed).Select(t => t.Result.s.Mod).ToList();
            var failedSvMods = tasks.Where(t => t.Result.result == UpdateResult.FailedSharingViolation).Select(t => t.Result.s.Mod).ToList();
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
                if (_lastProgress.Values.All(p => p.State == FileSyncState.None))
                    return new FileSyncStats(FileSyncState.None);
                ulong sumTotal = 0;
                ulong sumDone = 0;
                foreach (var mod in _lastProgress.Keys)
                {
                    var downloadTotal = _totalSizes[mod];
                    sumTotal += downloadTotal;

                    var (fileSyncState, _, done) = _lastProgress[mod];
                    if (fileSyncState == FileSyncState.Updating)
                    {
                        sumDone += done;
                    }
                    else
                    {
                        sumDone += downloadTotal;
                    }
                }

                return new FileSyncStats(FileSyncState.Updating, sumTotal, sumDone);
            }
        }

        private void ReportProgress(FileSyncStats stats)
        {
            _logger.Trace($"Progress: {stats.State}");
            _progress?.Report(stats);
        }

        internal static async Task<StageStats> Update(List<ModUpdate> updates, IProgress<FileSyncStats> progress)
        {
            var update = new RepositoryUpdate(updates, progress);
            return await update.Update();
        }

        private RepositoryUpdate(List<ModUpdate> updates, IProgress<FileSyncStats> progress)
        {
            _logger = LogHelper.GetLoggerWithIdentifier(this, Guid.NewGuid().ToString());
            _updates = updates;
            _progress = progress;
            foreach (var (_, modProgress, mod) in updates)
            {
                _lastProgress.Add(mod, new FileSyncStats(FileSyncState.Waiting));
                _totalSizes.Add(mod, 0);
                modProgress.ProgressChanged += (_, e) => ModProgressOnProgressChanged(e, mod);
            }

            _logger.Trace("Progress: Waiting");
            _progress?.Report(new FileSyncStats(FileSyncState.Waiting));
        }

        private void ModProgressOnProgressChanged(FileSyncStats progress, IModelStorageMod mod)
        {
            lock (_progressLock)
            {
                _lastProgress[mod] = progress;
                if (progress.State == FileSyncState.Updating)
                    _totalSizes[mod] = progress.Total;
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
