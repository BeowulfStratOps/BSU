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
        }
    }
}