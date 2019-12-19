using System;
using System.Collections.Generic;
using System.Text;
using BSU.CoreCommon;

namespace BSU.Core.Sync
{
    internal abstract class WorkUnit
    {
        protected readonly ILocalMod Local;
        protected readonly string Path;
        private readonly RepoSync _sync;
        private bool _done;

        protected WorkUnit(ILocalMod local, string path, RepoSync sync)
        {
            Local = local;
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
