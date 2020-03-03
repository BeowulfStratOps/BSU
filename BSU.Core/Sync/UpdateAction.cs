using System.IO;
using System.Threading;
using BSU.Core.Model;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Sync
{
    /// <summary>
    /// WorkUnit: Updates a single file from a remote location. Refers to the RepositoryMod for the actual operation.
    /// </summary>
    internal class UpdateAction : SyncWorkUnit
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly RepositoryMod _repository;
        private readonly long _sizeTotal;
        private long _sizeTodo;

        public UpdateAction(RepositoryMod repository, StorageMod storage, string path, long sizeTotal, RepoSync sync)
            : base(storage, path, sync)
        {
            _repository = repository;
            _sizeTotal = sizeTotal;
            _sizeTodo = sizeTotal;
        }

        public long GetBytesTotal() => _sizeTotal;
        public long GetBytesRemaining() => _sizeTodo;

        protected override void DoWork(CancellationToken token)
        {
            Logger.Trace("{0}, {1} Updating {2}", Storage.Uid, _repository.Uid, Path);
            using var target = Storage.Implementation.OpenFile(Path.ToLowerInvariant(), FileAccess.ReadWrite);
            _repository.Implementation.UpdateTo(Path, target, UpdateRemaining, token);
            _sizeTodo = 0;
        }

        private void UpdateRemaining(long bytesDownloaded)
        {
            _sizeTodo -= bytesDownloaded;
        }
    }
}
