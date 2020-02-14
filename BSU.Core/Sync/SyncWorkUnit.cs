using System;
using System.Threading;
using BSU.Core.Model;
using BSU.CoreCommon;

namespace BSU.Core.Sync
{
    /// <summary>
    /// Base class for atomic sync operations. Tracks progress/state.
    /// </summary>
    internal abstract class SyncWorkUnit : WorkUnit
    {
        protected readonly StorageMod Storage;
        protected readonly string Path;
        private readonly RepoSync _sync;

        protected SyncWorkUnit(StorageMod storage, string path, RepoSync sync) : base(sync.GetCancellationToken())
        {
            Storage = storage;
            Path = path;

            _sync = sync;
        }
    }
}
