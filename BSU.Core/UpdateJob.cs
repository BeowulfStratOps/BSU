using System;
using BSU.Core.Sync;
using BSU.CoreCommon;

namespace BSU.Core
{
    public class UpdateJob
    {
        internal readonly ILocalMod LocalMod;
        internal readonly IRemoteMod RemoteMod;
        internal readonly UpdateTarget Target;
        internal readonly RepoSync SyncState;

        public delegate void JobEndedDelegate(bool success);
        public event JobEndedDelegate JobEnded;
        internal void SignalJobEnd(bool success) => JobEnded?.Invoke(success);

        internal UpdateJob(ILocalMod localMod, IRemoteMod remoteMod, UpdateTarget target, RepoSync syncState)
        {
            LocalMod = localMod;
            RemoteMod = remoteMod;
            Target = target;
            SyncState = syncState;
        }
    }
}
