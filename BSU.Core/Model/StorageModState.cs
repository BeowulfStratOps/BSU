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
        public readonly Exception Error;

        public StorageModState(MatchHash matchHash, VersionHash versionHash, UpdateTarget updateTarget,
            UpdateTarget jobTarget, StorageModStateEnum state, Exception error)
        {
            MatchHash = matchHash;
            VersionHash = versionHash;
            UpdateTarget = updateTarget;
            JobTarget = jobTarget;
            State = state;
            Error = error;
            CheckIsValid();
        }

        private void CheckIsValid()
        {
            switch (State)
            {
                case StorageModStateEnum.CreatedWithUpdateTarget:
                    Assert(VersionHash == null, nameof(VersionHash));
                    Assert(MatchHash == null, nameof(MatchHash));
                    Assert(JobTarget == null, nameof(JobTarget));
                    Assert(UpdateTarget != null, nameof(UpdateTarget));
                    Assert(Error == null, nameof(Error));
                    return;
                case StorageModStateEnum.CreatedForDownload:
                    Assert(VersionHash == null, nameof(VersionHash));
                    Assert(MatchHash == null, nameof(MatchHash));
                    Assert(JobTarget == null, nameof(JobTarget));
                    Assert(UpdateTarget != null, nameof(UpdateTarget));
                    Assert(Error == null, nameof(Error));
                    return;
                case StorageModStateEnum.Loading:
                    Assert(VersionHash == null, nameof(VersionHash));
                    //Assert(MatchHash == null, nameof(MatchHash)); // TODO: re-enable
                    Assert(JobTarget == null, nameof(JobTarget));
                    Assert(UpdateTarget == null, nameof(UpdateTarget));
                    Assert(Error == null, nameof(Error));
                    return;
                case StorageModStateEnum.Loaded:
                    Assert(VersionHash == null, nameof(VersionHash));
                    Assert(MatchHash != null, nameof(MatchHash));
                    Assert(JobTarget == null, nameof(JobTarget));
                    Assert(UpdateTarget == null, nameof(UpdateTarget));
                    Assert(Error == null, nameof(Error));
                    return;
                case StorageModStateEnum.Hashing:
                    Assert(VersionHash == null, nameof(VersionHash));
                    Assert(MatchHash != null, nameof(MatchHash));
                    Assert(JobTarget == null, nameof(JobTarget));
                    Assert(UpdateTarget == null, nameof(UpdateTarget));
                    Assert(Error == null, nameof(Error));
                    return;
                case StorageModStateEnum.Hashed:
                    Assert(VersionHash != null, nameof(VersionHash));
                    Assert(MatchHash != null, nameof(MatchHash));
                    Assert(JobTarget == null, nameof(JobTarget));
                    Assert(UpdateTarget == null, nameof(UpdateTarget));
                    Assert(Error == null, nameof(Error));
                    return;
                case StorageModStateEnum.Updating:
                    Assert(VersionHash == null, nameof(VersionHash));
                    Assert(MatchHash == null, nameof(MatchHash));
                    Assert(JobTarget != null, nameof(JobTarget));
                    Assert(UpdateTarget != null, nameof(UpdateTarget));
                    Assert(Error == null, nameof(Error));
                    return;
                case StorageModStateEnum.ErrorLoad:
                    Assert(VersionHash == null, nameof(VersionHash));
                    Assert(MatchHash == null, nameof(MatchHash));
                    Assert(JobTarget == null, nameof(JobTarget));
                    Assert(UpdateTarget == null, nameof(UpdateTarget));
                    Assert(Error != null, nameof(Error));
                    return;
                case StorageModStateEnum.ErrorUpdate:
                    Assert(VersionHash == null, nameof(VersionHash));
                    Assert(MatchHash == null, nameof(MatchHash));
                    Assert(JobTarget == null, nameof(JobTarget));
                    Assert(UpdateTarget != null, nameof(UpdateTarget));
                    Assert(Error != null, nameof(Error));
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Assert(bool statement, string argument)
        {
            if (!statement) throw new InvalidOperationException(argument);
        }
    }
}