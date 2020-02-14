using System.Threading;
using BSU.Core.Model;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Sync
{
    /// <summary>
    /// WorkUnit: Deletes a single file.
    /// </summary>
    internal class DeleteAction : SyncWorkUnit
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public DeleteAction(StorageMod storage, string path, RepoSync sync) : base(storage, path, sync)
        {
        }

        protected override void DoWork(CancellationToken token)
        {
            Logger.Trace("{0} Deleting {1}", Storage.Uid, Path);
            Storage.Implementation.DeleteFile(Path);
        }
    }
}
