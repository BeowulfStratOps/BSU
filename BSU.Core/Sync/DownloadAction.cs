using System.IO;
using System.Threading;
using BSU.Core.Model;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Sync
{
    /// <summary>
    /// WorkUnit: Downloads a new file. Refers to the RepositoryMod for the actual download.
    /// </summary>
    internal class DownloadAction : SyncWorkUnit
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IRepositoryMod _repository;
        private readonly long _sizeTotal;
        private long _sizeTodo;

        public DownloadAction(IRepositoryMod repository, StorageMod storage, string path, long sizeTotal,
            RepoSync sync) : base(storage, path, sync)
        {
            _repository = repository;
            _sizeTotal = sizeTotal;
            _sizeTodo = sizeTotal;
        }

        public long GetBytesTotal() => _sizeTotal;
        public long GetBytesRemaining() => _sizeTodo;

        protected override void DoWork(CancellationToken token)
        {
            Logger.Trace("{0}, {1} Downloading {2}", _repository.GetUid(), _repository.GetUid(), Path);
            using var target = Storage.Implementation.OpenFile(Path.ToLowerInvariant(), FileAccess.Write);
            _repository.DownloadTo(Path, target, UpdateRemaining, token);
            Thread.Sleep(2000);
            _sizeTodo = 0;
        }

        private void UpdateRemaining(long bytesDownloaded)
        {
            _sizeTodo -= bytesDownloaded;
        }
    }
}
