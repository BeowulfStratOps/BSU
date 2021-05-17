using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.Model.Utility;

namespace BSU.Core.Model.Updating
{
    public class RepositoryUpdateCommon
    {
        public readonly ProgressProvider ProgressProvider = new();

        public event Action OnEnded;

        public void SignalEnded()
        {
            OnEnded?.Invoke();
        }
    }

    public abstract class RepositoryUpdateBase
    {
        private readonly IEnumerable<IUpdateState> _updates;
        private bool _invalidatedState;
        protected readonly RepositoryUpdateCommon Common;

        protected RepositoryUpdateBase(IEnumerable<IUpdateState> updates, RepositoryUpdateCommon common)
        {
            _updates = updates;
            Common = common;
        }

        protected ProgressProvider Progress => Common.ProgressProvider;
        public IProgressProvider ProgressProvider => Common.ProgressProvider;

        protected void InvalidateState()
        {
            if (_invalidatedState) throw new InvalidOperationException();
            _invalidatedState = true;
        }

        public void Abort()
        {
            InvalidateState();

            foreach (var updateState in _updates)
            {
                updateState.Abort();
            }

            SignalEnded();
        }

        public event Action OnEnded
        {
            add => Common.OnEnded += value;
            remove => Common.OnEnded -= value;
        }

        protected void SignalEnded() => Common.SignalEnded();

        protected static async Task<(List<T> next, int failedCount)> DoStageAsync<T>(IReadOnlyCollection<Task<T>> tasks)
        {
            try
            {
                // this will continue once all tasks are done, regardless of exceptions
                await Task.WhenAll(tasks);
            }
            catch (Exception)
            {
                // we'll deal with any exceptions manually
            }

            var next = tasks.Where(t => t.IsCompletedSuccessfully).Select(t => t.Result).ToList();
            var failedCount = tasks.Count(t => t.IsFaulted);
            return (next, failedCount);
        }
    }

    public class RepositoryUpdate : RepositoryUpdateBase
    {
        private readonly List<IUpdateCreate> _updates;

        private void UpdateProgress()
        {
            Progress.IsIndeterminate = _updates.Any(u => u.ProgressProvider.IsIndeterminate);
            Progress.Value = _updates.Sum(u => u.ProgressProvider.Value) / _updates.Count;
        }

        public async Task<RepositoryUpdateCreated> Create()
        {
            InvalidateState();

            foreach (var update in _updates)
            {
                update.ProgressProvider.PropertyChanged += (_, _) => UpdateProgress();
            }

            var tasks = _updates.Select(s => s.Create()).ToList();
            var (next, failed) = await DoStageAsync(tasks);
            var stats = new StageStats<IUpdateCreated>(next, failed);
            return new RepositoryUpdateCreated(next, stats, Common);
        }

        public RepositoryUpdate(List<IUpdateCreate> updates) : base(updates, new RepositoryUpdateCommon())
        {
            _updates = updates;
        }
    }

    public class RepositoryUpdateCreated : RepositoryUpdateBase
    {
        public StageStats<IUpdateCreated> Stats { get; }
        private readonly IEnumerable<IUpdateCreated> _updates;

        public async Task<RepositoryUpdatePrepared> Prepare()
        {
            InvalidateState();

            var tasks = _updates.Select(s => s.Prepare()).ToList();
            var (next, failed) = await DoStageAsync(tasks);
            var stats = new StageStats<IUpdatePrepared>(next, failed);
            return new RepositoryUpdatePrepared(next, stats, Common);
        }

        public RepositoryUpdateCreated(List<IUpdateCreated> updates, StageStats<IUpdateCreated> stats,
            RepositoryUpdateCommon common) : base(updates, common)
        {
            Stats = stats;
            _updates = updates;
        }
    }

public class RepositoryUpdatePrepared : RepositoryUpdateBase
    {
        public StageStats<IUpdatePrepared> Stats { get; }
        private readonly List<IUpdatePrepared> _updates;

        public RepositoryUpdatePrepared(List<IUpdatePrepared> updates, StageStats<IUpdatePrepared> stats,
            RepositoryUpdateCommon common) : base(updates, common)
        {
            Stats = stats;
            _updates = updates;
        }

        public async Task<RepositoryUpdateDone> Update()
        {
            // TODO: quite a bit of code duplication going on here
            InvalidateState();

            var tasks = _updates.Select(s => s.Update()).ToList();
            var (next, failed) = await DoStageAsync(tasks);
            var stats = new StageStats<IUpdateDone>(next, failed);
            return new RepositoryUpdateDone(stats);
        }
    }

    public class RepositoryUpdateDone
    {
        public StageStats<IUpdateDone> Stats { get; }

        public RepositoryUpdateDone(StageStats<IUpdateDone> stats)
        {
            Stats = stats;
        }
    }

    public class StageStats<T>
    {
        public List<T> Succeeded { get; }
        public int FailedCount { get; }

        public StageStats(List<T> succeeded, int failed)
        {
            Succeeded = succeeded;
            FailedCount = failed;
        }
    }
}
