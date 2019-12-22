using System;
using System.Collections.Generic;
using System.Text;
using BSU.CoreCommon;

namespace BSU.Core.Sync
{
    internal abstract class WorkUnit
    {
        protected readonly IStorageMod Storage;
        protected readonly string Path;
        private readonly RepoSync _sync;
        private bool _done;

        protected WorkUnit(IStorageMod storage, string path, RepoSync sync)
        {
            Storage = storage;
            Path = path;
            _sync = sync;
        }

        private Exception _error;

        public void Work()
        {
            DoWork();
            _done = true;
            _sync.CheckDone();
        }

        protected abstract void DoWork();
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
