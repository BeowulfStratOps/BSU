using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;

namespace BSU.Core.Concurrency
{
    // TODO: create derived types without reset and/or without T
    // TODO: tests!
    internal class ResettableLazyAsync<T> where T : class
    {
        private readonly Func<CancellationToken, Task<T>> _function;
        private readonly CancellationToken _parentCancellationToken;
        private CancellationTokenSource _cts;

        // used for very short-lived lock. won't use cancellation token for that reason
        private readonly SemaphoreSlim _semaphore = new(1);

        private Task<T> _task;

        public ResettableLazyAsync(Func<CancellationToken, Task<T>> function, T initialValue = null, CancellationToken parentCancellationToken = default)
        {
            _function = function;
            _parentCancellationToken = parentCancellationToken;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(parentCancellationToken);
            if (initialValue != null)
                _task = Task.FromResult(initialValue);
        }

        public async Task AwaitReset()
        {
            await _semaphore.WaitAsync(CancellationToken.None);
            try
            {
                if (_task == null) return;
                _cts.Cancel();
                _cts = CancellationTokenSource.CreateLinkedTokenSource(_parentCancellationToken);
                try
                {
                    await _task;
                }
                catch
                {
                    // ignored
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<T> Get()
        {
            // TODO: cancellation?
            await _semaphore.WaitAsync(CancellationToken.None);
            try
            {
                if (_task != null) return await _task;
                var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                _task = Task.Run(async () => await _function(cts.Token), cts.Token);
                return await _task;
            }
            finally
            {
                _semaphore.Release();
            }
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
}
