using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using BSU.Core.Hashes;

namespace BSU.Core.Tests
{
    public class TestUtils
    {
        internal static VersionHash GetVersionHash(string version)
        {
            if (version == null) return null;
            using var sha1 = SHA1.Create();
            return new VersionHash(sha1.ComputeHash(Encoding.UTF8.GetBytes(version)));
        }
        
        internal static UpdateTarget GetUpdateTarget(string version)
        {
            if (version == null) return null;
            return new UpdateTarget(GetVersionHash(version).GetHashString(), null);
        }
        
        internal static MatchHash GetMatchHash(string match)
        {
            if (match == null) return null;
            return new MatchHash(new[] {match});
        }
    }
}