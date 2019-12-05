using System;
using System.Threading;
using BSU.CoreInterface;

namespace BSU.Core.Sync
{
    internal class DeleteAction : WorkUnit
    {
        public DeleteAction(ILocalMod local, string path) : base(local, path)
        {

        }

        public override void DoWork()
        {
            _local.DeleteFile(_path);
            _done = true;
        }
    }
}