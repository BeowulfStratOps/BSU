using System;
using System.Threading;
using BSU.CoreCommon;

namespace BSU.Core.Sync
{
    /// <summary>
    /// Base class for atomic sync operations. Tracks progress/state.
    /// </summary>
    internal abstract class WorkUnit
    {
        protected readonly IStorageMod Storage;
        protected readonly string Path;
        private readonly RepoSync _sync;
        private readonly CancellationToken _token;
        private bool _done;

        protected WorkUnit(IStorageMod storage, string path, RepoSync sync)
        {
            Storage = storage;
            Path = path;
            _sync = sync;
            _token = sync.GetCancellationToken();
        }

        private Exception _error;

        public void Work()
        {
            DoWork(_token);
            _done = true;
            _sync.CheckDone(); // TODO: use better work-unit tracking!
        }

        protected abstract void DoWork(CancellationToken token);
        public bool IsDone() => _done;

        internal void SetError(Exception e)
        {
            _error = e;
            _sync.CheckDone();
        }

        public bool HasError() => _error != null;
        public Exception GetError() => _error;
    }
}
