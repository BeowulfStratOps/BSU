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
        private ModAction GetAction(string repoModVer, string storageModVer, string updatingTo, string job, bool canWrite)
        {
            var repoHash = TestUtils.GetVersionHash(repoModVer);
            var storageHash = TestUtils.GetVersionHash(storageModVer);
            var jobTarget = TestUtils.GetUpdateTarget(job);
            var updatingTarget = TestUtils.GetUpdateTarget(updatingTo);
            var repoState = new RepositoryModState(null, repoHash);
            var storageState = new StorageModState(null, storageHash, updatingTarget, jobTarget, true);
            return CoreCalculation.CalculateAction(repoState, storageState, canWrite);
        }
        
        [Fact]
        private void SimpleUse()
        {
            Assert.Equal(ModAction.Use, GetAction("1", "1", null, null, true));
        }

        [Fact]
        private void ContinueUpdate()
        {
            var action = GetAction("1", "?", "1", null, true);
            Assert.Equal(ModAction.ContinueUpdate, action);
        }
        
        [Fact]
        private void AbortAndUpdate()
        {
            var action = GetAction("2", "?", "1", "1", true);
            Assert.Equal(ModAction.AbortAndUpdate, action);
        }
    }
}