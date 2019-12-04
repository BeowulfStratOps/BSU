using BSU.CoreInterface;

namespace BSU.Core
{
    public class UpdateJob
    {
        public readonly ILocalMod LocalMod;
        public readonly IRemoteMod RemoteMod;
        public readonly UpdateTarget Target;
        public readonly ISyncState SyncState;

        public UpdateJob(ILocalMod localMod, IRemoteMod remoteMod, UpdateTarget target, ISyncState syncState)
        {
            LocalMod = localMod;
            RemoteMod = remoteMod;
            Target = target;
            SyncState = syncState;
        }
    }
}