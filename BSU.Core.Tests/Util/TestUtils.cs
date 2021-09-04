using System.Security.Cryptography;
using System.Text;
using System.Threading;
using BSU.Core.Hashes;
using BSU.Core.Tests.Mocks;

namespace BSU.Core.Tests.Util
{
    internal static class TestUtils
    {
        internal static MatchHash GetMatchHash(int? match)
        {
            if (match == null) return null;
            var mockRepo = new MockRepositoryMod();
            for (int i = 0; i < 3; i++)
            {
                mockRepo.SetFile($"/addons/{match}_{i}.pbo", i.ToString());
            }
            return MatchHash.CreateAsync(mockRepo, CancellationToken.None).Result;
        }

        internal static VersionHash GetVersionHash(int? version)
        {
            if (version == null) return null;
            var mockRepo = new MockRepositoryMod();
            for (int i = 0; i < 3; i++)
            {
                mockRepo.SetFile($"/addons/file_{i}.pbo", version.ToString() + i);
            }
            return VersionHash.CreateAsync(mockRepo, CancellationToken.None).Result;
        }
    }
}
