using BSU.Core.Hashes;

namespace BSU.Core.Model
{
    internal class RepositoryModState
    {
        public readonly MatchHash MatchHash;
        public readonly VersionHash VersionHash;

        public RepositoryModState(MatchHash matchHash, VersionHash versionHash)
        {
            MatchHash = matchHash;
            VersionHash = versionHash;
        }

        public bool IsLoading => MatchHash == null || VersionHash == null;
    }
}