using System.Threading;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Sync
{
    /// <summary>
    /// WorkUnit: Deletes a single file.
    /// </summary>
    internal class DeleteAction : WorkUnit
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public DeleteAction(IStorageMod storage, string path, RepoSync sync) : base(storage, path, sync)
        {
        }

        protected override void DoWork(CancellationToken token)
        {
            Logger.Trace("{0} Deleting {1}", Storage.GetUid(), Path);
            Storage.DeleteFile(Path);
        }
    }
}
