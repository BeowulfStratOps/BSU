using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Tests.Mocks;

namespace BSU.Core.Tests.Util
{
    internal static class TestUtils
    {
        internal static async Task<MatchHash> GetMatchHash(int? match)
        {
            if (match == null) return null;
            var mockRepo = new MockRepositoryMod();
            for (int i = 0; i < 3; i++)
            {
                mockRepo.SetFile($"/addons/{match}_{i}.pbo", i.ToString());
            }
            return await MatchHash.CreateAsync(mockRepo, CancellationToken.None);
        }

        internal static async Task<VersionHash> GetVersionHash(int? version)
        {
            if (version == null) return null;
            var mockRepo = new MockRepositoryMod();
            for (int i = 0; i < 3; i++)
            {
                mockRepo.SetFile($"/addons/file_{i}.pbo", version.ToString() + i);
            }
            return await VersionHash.CreateAsync(mockRepo, CancellationToken.None);
        }
    }
}
