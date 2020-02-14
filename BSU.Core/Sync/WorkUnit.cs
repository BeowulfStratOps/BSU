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
        private bool _done;
        private readonly CancellationToken _token;

        protected WorkUnit(CancellationToken tokenGetter)
        {
            _token = tokenGetter;
        }

        private Exception _error;

        public virtual void Work()
        {
            DoWork(_token);
            _done = true;
        }

        protected abstract void DoWork(CancellationToken token);
        public bool IsDone() => _done;

        internal void SetError(Exception e)
        {
            _error = e;
        }

        public bool HasError() => _error != null;
        public Exception GetError() => _error;
    }
}
