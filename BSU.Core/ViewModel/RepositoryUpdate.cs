using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Model.Updating;
using BSU.Core.Sync;
using NLog;

namespace BSU.Core.ViewModel
{

    public class RepositoryUpdate : IRepositoryUpdate
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

            var tasks = _updates.Select(s => s.update.Prepare(cancellationToken)).ToList();

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

            return new StageStats(tasks.Count(t => t.IsCompletedSuccessfully), tasks.Count(t => t.IsFaulted));
        }

        public async Task<StageStats> Update(CancellationToken cancellationToken)
        {
            if (_updated) throw new InvalidOperationException();
            _updated = true;

            cancellationToken.Register(() => ReportProgress(new FileSyncStats(FileSyncState.Stopping)));

            var tasks = _updates.Where(u => u.update.IsPrepared).Select(s => s.update.Update(cancellationToken)).ToList();
            var whenAll = Task.WhenAll(tasks);

            try
            {
                await whenAll.WithUpdates(TimeSpan.FromMilliseconds(50), () => ReportProgress(GetProgress()));
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
            finally
            {
                ReportProgress(new FileSyncStats(FileSyncState.None));
            }

            return new StageStats(tasks.Count(t => t.IsCompletedSuccessfully), tasks.Count(t => t.IsFaulted));
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
                modProgress.ProgressChanged += (s, e) => ModProgressOnProgressChanged(s, e, update);
            }

            _logger.Trace("Progress Waiting");
            _progress?.Report(new FileSyncStats(FileSyncState.Waiting));
        }

        private void ModProgressOnProgressChanged(object sender, FileSyncStats progress, IModUpdate update)
        {
            lock (_progressLock)
            {
                _lastProgress[update] = progress;
            }
        }
    }

    public class StageStats
    {
        public int SucceededCount { get; }
        public int FailedCount { get; }

        public StageStats(int succeededCount, int failed)
        {
            SucceededCount = succeededCount;
            FailedCount = failed;
        }
    }
}
