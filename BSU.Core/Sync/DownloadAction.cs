using System.IO;
using System.Threading;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Sync
{
    internal class DownloadAction : WorkUnit
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IRepositoryMod _repository;
        private readonly long _sizeTotal;
        private long _sizeTodo;

        public DownloadAction(IRepositoryMod repository, IStorageMod storage, string path, long sizeTotal, RepoSync sync) : base(storage, path, sync)
        {
            _repository = repository;
            _sizeTotal = sizeTotal;
            _sizeTodo = sizeTotal;
        }

        public long GetBytesTotal() => _sizeTotal;
        public long GetBytesRemaining() => _sizeTodo;

        protected override void DoWork()
        {
            Logger.Trace("Downloading {0}", Path);
            var target = Storage.GetFilePath(Path.ToLowerInvariant());
            var di = new FileInfo(target).Directory;
            if (!di.Exists) di.Create();
            _repository.DownloadTo(Path, target, UpdateRemaining);
            _sizeTodo = 0;
        }

        private void UpdateRemaining(long bytesDownloaded)
        {
            _sizeTodo -= bytesDownloaded;
        }
    }
}
