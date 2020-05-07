using System;
using BSU.Core.Hashes;

namespace BSU.Core.Model
{
    internal class StorageModState
    {
        public readonly MatchHash MatchHash;
        public readonly VersionHash VersionHash;
        public readonly UpdateTarget UpdateTarget, JobTarget; // TODO: clarify what's the difference
        public readonly StorageModStateEnum State;

        public StorageModState(MatchHash matchHash, VersionHash versionHash, UpdateTarget updateTarget,
            UpdateTarget jobTarget, StorageModStateEnum state)
        {
            MatchHash = matchHash;
            VersionHash = versionHash;
            UpdateTarget = updateTarget;
            JobTarget = jobTarget;
            State = state;
            CheckIsValid();
        }

        private void CheckIsValid()
        {
            switch (State)
            {
                case StorageModStateEnum.CreatedWithUpdateTarget:
                    Assert(VersionHash == null);
                    Assert(MatchHash == null);
                    Assert(JobTarget == null);
                    Assert(UpdateTarget != null);
                    return;
                case StorageModStateEnum.CreatedForDownload:
                    Assert(VersionHash == null);
                    Assert(MatchHash == null);
                    Assert(JobTarget == null);
                    Assert(UpdateTarget != null);
                    return;
                case StorageModStateEnum.Loading:
                    Assert(VersionHash == null);
                    Assert(MatchHash == null);
                    Assert(JobTarget == null);
                    return;
                case StorageModStateEnum.Loaded:
                    Assert(VersionHash == null);
                    Assert(MatchHash != null);
                    Assert(JobTarget == null);
                    return;
                case StorageModStateEnum.Hashing:
                    Assert(VersionHash == null);
                    Assert(MatchHash != null);
                    Assert(JobTarget == null);
                    return;
                case StorageModStateEnum.Hashed:
                    Assert(VersionHash != null);
                    Assert(MatchHash != null);
                    Assert(JobTarget == null);
                    Assert(UpdateTarget == null);
                    return;
                case StorageModStateEnum.Updating:
                    Assert(VersionHash == null);
                    Assert(MatchHash == null);
                    Assert(JobTarget != null);
                    Assert(UpdateTarget != null);
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Assert(bool statement)
        {
            if (!statement) throw new InvalidOperationException();
        }
    }
}