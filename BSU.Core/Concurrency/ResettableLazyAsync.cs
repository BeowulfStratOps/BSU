using System;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace BSU.Core.Concurrency
{
    // TODO: create derived types without reset and/or without T ?
    internal class ResettableLazyAsync<T> where T : class
    {
        private readonly Func<CancellationToken, Task<T>> _function;
        private readonly Action<JobLogEvent> _log;
        private CancellationTokenSource _cts;

        // used for very short-lived lock. won't use cancellation token for that reason
        private readonly SemaphoreSlim _semaphore = new(1);

        private Task<T> _task;

        public ResettableLazyAsync(Func<CancellationToken, Task<T>> function, T initialValue = null, Action<JobLogEvent> log = null)
        {
            _function = function;
            _log = log;
            _cts = new CancellationTokenSource();
            if (initialValue != null)
                _task = Task.FromResult(initialValue);
        }

        public async Task ResetAndWaitAsync()
        {
            await _semaphore.WaitAsync(CancellationToken.None);
            try
            {
                if (_task == null) return;
                if (_task.IsCompleted)
                {
                    _task = null;
                    return;
                }
                _cts.Cancel();
                _cts = new CancellationTokenSource();
                try
                {
                    await _task;
                    _task = null;
                }
                catch
                {
                    // ignored
                }
                _log?.Invoke(JobLogEvent.Abort);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="cancellationToken">Cancels the waiting, but never the task</param>
        /// <returns></returns>
        /// <exception cref="TaskCanceledException"></exception>
        public async Task<T> GetAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(CancellationToken.None);
            Task<T> task;
            try
            {
                if (_task != null)
                {
                    task = _task;
                }
                else
                {
                    var operationCancellationToken = _cts.Token;
                    _task = Task.Run(async () =>
                        {
                            _log?.Invoke(JobLogEvent.Start);
                            var result = await _function(operationCancellationToken);
                            _log?.Invoke(JobLogEvent.Stop);
                            return result;
                        },
                        operationCancellationToken);
                    task = _task;
                }
            }
            finally
            {
                _semaphore.Release();
            }

            await Task.WhenAny(task, cancellationToken.AsTask());
            cancellationToken.ThrowIfCancellationRequested();
            return task.Result;
        }

        public async Task Set(T value)
        {
            await _semaphore.WaitAsync(CancellationToken.None);
            try
            {
                if (_task != null) throw new InvalidOperationException();
                _task = Task.FromResult(value);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    internal enum JobLogEvent
    {
        Start,
        Stop,
        Abort
    }
}
