using System;
using System.Threading;
using BSU.CoreCommon;

namespace BSU.Core.Sync
{
    internal class DeleteAction : WorkUnit
    {
        public DeleteAction(ILocalMod local, string path, RepoSync sync) : base(local, path, sync)
        {

        }

        protected override void DoWork()
        {
            Local.DeleteFile(Path);
        }
    }
}
