using System.IO;
using System.Threading;
using BSU.CoreInterface;
using Microsoft.VisualBasic.CompilerServices;

namespace BSU.Core.Sync
{
    internal class UpdateAction : WorkUnit
    {
        private readonly IRemoteMod _remote;
        private readonly long _sizeTotal;
        private long _sizeTodo;

        public UpdateAction(IRemoteMod remote, ILocalMod local, string path, long sizeTotal) : base(local, path)
        {
            _remote = remote;
            _sizeTotal = sizeTotal;
            _sizeTodo = sizeTotal;
        }

        public long GetBytesTotal() => _sizeTotal;
        public long GetBytesRemaining() => _sizeTodo;

        public override void DoWork()
        {
            using var target = _local.OpenFile(_path, FileAccess.ReadWrite);
            _remote.UpdateTo(_path, target, UpdateRemaining);
            _sizeTodo = 0;
            _done = true;
        }

        void UpdateRemaining(long remaining)
        {
            _sizeTodo = remaining;
        }
    }
}