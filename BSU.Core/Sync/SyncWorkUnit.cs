using System;
using System.Threading;
using BSU.Core.Model;
using BSU.CoreCommon;

namespace BSU.Core.Sync
{
    /// <summary>
    /// Base class for atomic sync operations. Tracks progress/state.
    /// </summary>
    internal abstract class SyncWorkUnit
    {
        protected readonly StorageMod Storage;
        protected readonly string Path;
        private bool _done;

        protected SyncWorkUnit(StorageMod storage, string path, RepoSync sync)
        {
            Storage = storage;
            Path = path;
        }
        
        public virtual void Work(CancellationToken cancellationToken)
        {
            DoWork(cancellationToken);
            _done = true;
        }

        protected abstract void DoWork(CancellationToken token);
        public bool IsDone() => _done;
    }
}
