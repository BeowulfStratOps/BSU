using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model.Updating;
using BSU.Core.Sync;

namespace BSU.Core.ViewModel
{

    public class RepositoryUpdate : IRepositoryUpdate
    {
        private readonly List<(IModUpdate update, Progress<FileSyncStats> progress)> _updates;
        private readonly IProgress<FileSyncStats> _progress;

        private readonly Dictionary<IModUpdate, FileSyncStats> _lastProgress = new();
        private readonly object _progressLock = new();

        private bool _prepared, _updated;

        public async Task<StageStats> Prepare(CancellationToken cancellationToken)
        {
            if (_prepared) throw new InvalidOperationException();
            _prepared = true;

            var tasks = _updates.Select(s => s.update.Prepare(cancellationToken)).ToList();

            try
            {
                await Task.WhenAll(tasks);
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

            var tasks = _updates.Where(u => u.update.IsPrepared).Select(s => s.update.Update(cancellationToken)).ToList();
            var whenAll = Task.WhenAll(tasks);

            while (!whenAll.IsCompleted)
            {
                var wait = Task.Delay(50, cancellationToken);
                try
                {
                    await Task.WhenAny(wait, whenAll);
                    ReportProgress();
                }
                catch
                {
                    // ignored
                }
            }

            return new StageStats(tasks.Count(t => t.IsCompletedSuccessfully), tasks.Count(t => t.IsFaulted));
        }

        private FileSyncStats GetProgress()
        {
            lock (_progressLock)
            {
                if (_lastProgress.Values.Any(p => p.State == FileSyncState.Waiting))
                    return new FileSyncStats(FileSyncState.Waiting, 0, 0, 0, 0);
                var state = _lastProgress.Values.All(p => p.State == FileSyncState.None)
                    ? FileSyncState.None
                    : FileSyncState.Updating;
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

                return new FileSyncStats(state, sumDownloadTotal, sumUpdateTotal, sumDownloadDone, sumUpdateDone);
            }
        }

        private void ReportProgress()
        {
            var state = GetProgress();
            _progress.Report(state);
        }

        internal RepositoryUpdate(List<(IModUpdate update, Progress<FileSyncStats> progress)> updates, IProgress<FileSyncStats> progress)
        {
            _updates = updates;
            _progress = progress;
            foreach (var (update, modProgress) in updates)
            {
                _lastProgress.Add(update, new FileSyncStats(FileSyncState.Waiting, 0,0,0,0));
                modProgress.ProgressChanged += (s, e) => ModProgressOnProgressChanged(s, e, update);
            }

            _progress?.Report(new FileSyncStats(FileSyncState.Waiting, 0, 0, 0, 0));
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
