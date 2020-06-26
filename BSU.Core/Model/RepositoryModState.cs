using System;
using BSU.Core.Hashes;

namespace BSU.Core.Model
{
    internal class RepositoryModState
    {
        public readonly MatchHash MatchHash;
        public readonly VersionHash VersionHash;
        public readonly Exception Error;

        public RepositoryModState(MatchHash matchHash, VersionHash versionHash, Exception error)
        {
            MatchHash = matchHash;
            VersionHash = versionHash;
            Error = error;
            
            if (error != null && (MatchHash != null || VersionHash != null)) throw new InvalidOperationException();
        }

        public bool IsLoading => MatchHash == null || VersionHash == null;
    }
}