using System;
using BSU.Core.Hashes;

namespace BSU.Core.Model
{
    internal class StorageModState
    {
        public readonly MatchHash MatchHash;
        public readonly VersionHash VersionHash;
        public readonly UpdateTarget UpdateTarget, JobTarget; // TODO: clarify what's the difference
        public readonly bool VersionHashRequested;

        public StorageModState(MatchHash matchHash, VersionHash versionHash, UpdateTarget updateTarget,
            UpdateTarget jobTarget, bool versionHashRequested)
        {
            MatchHash = matchHash;
            VersionHash = versionHash;
            UpdateTarget = updateTarget;
            JobTarget = jobTarget;
            VersionHashRequested = versionHashRequested;
            CheckIsValid();
        }
        
        public bool IsLoading => MatchHash == null;
        public bool IsHashing => VersionHash == null && VersionHashRequested;
        public bool IsUpdating => JobTarget != null; // TODO: job or updatetarget??

        private void CheckIsValid()
        {
            if (JobTarget != null)
            {
                if (JobTarget.Hash != UpdateTarget?.Hash) throw new InvalidOperationException();
            }
        }
    }
}