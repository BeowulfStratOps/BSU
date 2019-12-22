using System;
using BSU.Core.Sync;
using BSU.CoreCommon;

namespace BSU.Core
{
    public class UpdateJob
    {
        internal readonly IStorageMod StorageMod;
        internal readonly IRepositoryMod RepositoryMod;
        internal readonly UpdateTarget Target;
        internal readonly RepoSync SyncState;

        internal UpdateJob(IStorageMod storageMod, IRepositoryMod repositoryMod, UpdateTarget target, RepoSync syncState)
        {
            StorageMod = storageMod;
            RepositoryMod = repositoryMod;
            Target = target;
            SyncState = syncState;
        }
    }
}
