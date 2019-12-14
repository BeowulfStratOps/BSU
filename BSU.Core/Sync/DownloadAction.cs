using System.IO;
using System.Threading;
using BSU.CoreInterface;

namespace BSU.Core.Sync
{
    internal class DownloadAction : WorkUnit
    {
        private readonly IRemoteMod _remote;
        private readonly long _sizeTotal;
        private long _sizeTodo;

        public DownloadAction(IRemoteMod remote, ILocalMod local, string path, long sizeTotal) : base(local, path)
        {
            _remote = remote;
            _sizeTotal = sizeTotal;
            _sizeTodo = sizeTotal;
        }

        public long GetBytesTotal() => _sizeTotal;
        public long GetBytesRemaining() => _sizeTodo;

        public override void DoWork()
        {
            var target = _local.GetFilePath(_path);
            _remote.DownloadTo(_path, target, UpdateRemaining);
            _sizeTodo = 0;
            _done = true;
        }

        private void UpdateRemaining(long bytesDownloaded)
        {
            _sizeTodo -= bytesDownloaded;
        }
    }
}