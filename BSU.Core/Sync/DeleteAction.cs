using System;
using System.Threading;
using BSU.CoreCommon;

namespace BSU.Core.Sync
{
    internal class DeleteAction : WorkUnit
    {
        public DeleteAction(IStorageMod storage, string path, RepoSync sync) : base(storage, path, sync)
        {

        }

        protected override void DoWork()
        {
            Storage.DeleteFile(Path);
        }
    }
}
