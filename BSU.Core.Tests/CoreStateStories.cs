using System.Security.Cryptography;
using System.Text;
using BSU.Core.Hashes;
using BSU.Core.Model;
using Xunit;

namespace BSU.Core.Tests
{
    // TODO: extend for ViewState
    public class CoreStateStories
    {
        private VersionHash GetVersionHash(string version)
        {
            using var sha1 = SHA1.Create();
            return new VersionHash(sha1.ComputeHash(Encoding.UTF8.GetBytes(version)));
        }
        
        private ModAction GetAction(string repoModVer, string storageModVer, string updatingTo, string job, bool canWrite)
        {
            var repoHash = repoModVer == null ? null : GetVersionHash(repoModVer);
            var storageHash = repoModVer == null ? null : GetVersionHash(storageModVer);
            var jobTarget = job == null ? null : new UpdateTarget(GetVersionHash(job).GetHashString(), job);
            var updatingTarget = updatingTo == null ? null : new UpdateTarget(GetVersionHash(updatingTo).GetHashString(), updatingTo);
            return CalcAction.CalculateAction(repoHash, storageHash, jobTarget, updatingTarget, canWrite);
        }
        
        [Fact]
        private void SimpleUse()
        {
            Assert.Equal(ModAction.Use, GetAction("1", "1", null, null, true));
        }
    }
}