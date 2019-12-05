using System;
using BSU.Core.Sync;
using BSU.CoreInterface;

namespace BSU.Core
{
    public class UpdateJob
    {
        internal readonly ILocalMod LocalMod;
        internal readonly IRemoteMod RemoteMod;
        internal readonly UpdateTarget Target;
        internal readonly RepoSync SyncState;

        internal UpdateJob(ILocalMod localMod, IRemoteMod remoteMod, UpdateTarget target, RepoSync syncState)
        {
            LocalMod = localMod;
            RemoteMod = remoteMod;
            Target = target;
            SyncState = syncState;
        }
    }
}