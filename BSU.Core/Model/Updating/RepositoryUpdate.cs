using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BSU.Core.Model.Updating
{
    public abstract class RepositoryUpdateBase
    {
        private bool _invalidatedState;

        protected void InvalidateState()
        {
            if (_invalidatedState) throw new InvalidOperationException();
            _invalidatedState = true;
        }

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
        private readonly IEnumerable<IUpdateCreated> _updates;

        public async Task<RepositoryUpdatePrepared> Prepare(CancellationToken cancellationToken)
        {
            InvalidateState();

            var tasks = _updates.Select(s => s.Prepare(cancellationToken)).ToList();
            var (next, failed) = await DoStageAsync(tasks);
            var stats = new StageStats<IUpdatePrepared>(next, failed);
            return new RepositoryUpdatePrepared(next, stats);
        }

        public RepositoryUpdate(List<IUpdateCreated> updates)
        {
            _updates = updates;
        }
    }

public class RepositoryUpdatePrepared : RepositoryUpdateBase
    {
        public StageStats<IUpdatePrepared> Stats { get; }
        private readonly List<IUpdatePrepared> _updates;

        public RepositoryUpdatePrepared(List<IUpdatePrepared> updates, StageStats<IUpdatePrepared> stats)
        {
            Stats = stats;
            _updates = updates;
        }

        public async Task<RepositoryUpdateDone> Update(CancellationToken cancellationToken)
        {
            // TODO: quite a bit of code duplication going on here
            InvalidateState();

            var tasks = _updates.Select(s => s.Update(cancellationToken)).ToList();
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
