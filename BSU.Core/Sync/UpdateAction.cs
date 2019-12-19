using System.IO;
using System.Threading;
using BSU.CoreCommon;
using Microsoft.VisualBasic.CompilerServices;

namespace BSU.Core.Sync
{
    internal class UpdateAction : WorkUnit
    {
        private readonly IRemoteMod _remote;
        private readonly long _sizeTotal;
        private long _sizeTodo;

        public UpdateAction(IRemoteMod remote, ILocalMod local, string path, long sizeTotal, RepoSync sync) : base(local, path, sync)
        {
            _remote = remote;
            _sizeTotal = sizeTotal;
            _sizeTodo = sizeTotal;
        }

        public long GetBytesTotal() => _sizeTotal;
        public long GetBytesRemaining() => _sizeTodo;

        protected override void DoWork()
        {
            var target = Local.GetFilePath(Path.ToLowerInvariant());
            _remote.UpdateTo(Path, target, UpdateRemaining);
            _sizeTodo = 0;
        }

        void UpdateRemaining(long bytesDownloaded)
        {
            _sizeTodo -= bytesDownloaded;
        }
    }
}
