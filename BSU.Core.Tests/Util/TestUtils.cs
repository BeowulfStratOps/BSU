using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.Core.Services;
using BSU.Core.Tests.Mocks;
using Moq;
using Xunit;

namespace BSU.Core.Tests.Util
{
    internal static class TestUtils
    {
        internal static async Task<MatchHash> GetMatchHash(int match)
        {
            var mockRepo = new MockRepositoryMod();
            for (int i = 0; i < 3; i++)
            {
                mockRepo.SetFile($"/addons/{match}_{i}.pbo", i.ToString());
            }
            return await MatchHash.CreateAsync(mockRepo, CancellationToken.None);
        }

        internal static async Task<VersionHash> GetVersionHash(int version)
        {
            var mockRepo = new MockRepositoryMod();
            for (int i = 0; i < 3; i++)
            {
                mockRepo.SetFile($"/addons/file_{i}.pbo", version.ToString() + i);
            }
            return await VersionHash.CreateAsync(mockRepo, CancellationToken.None);
        }

        public static IModelStorageMod StorageModFromAction(ModActionEnum action, int repoModMatch = 1, int repoModVersion = 1)
        {
            var mock = new Mock<IModelStorageMod>(MockBehavior.Strict);

            var (state, match, version) = action switch
            {
                ModActionEnum.Use => (StorageModStateEnum.Created, repoModMatch, repoModVersion),
                ModActionEnum.Update => (StorageModStateEnum.Created, repoModMatch, repoModVersion - 1),
                ModActionEnum.ContinueUpdate => (StorageModStateEnum.CreatedWithUpdateTarget, repoModMatch, repoModVersion),
                ModActionEnum.Await => (StorageModStateEnum.Updating, repoModMatch, repoModVersion),
                ModActionEnum.AbortAndUpdate => (StorageModStateEnum.CreatedWithUpdateTarget, repoModMatch, repoModVersion - 1),
                ModActionEnum.Unusable => (StorageModStateEnum.Created, repoModMatch - 1, repoModVersion),
                ModActionEnum.AbortActiveAndUpdate => (StorageModStateEnum.Updating, repoModMatch, repoModVersion - 1),
                ModActionEnum.Loading => (StorageModStateEnum.Loading, repoModMatch, repoModVersion),
                ModActionEnum.UnusableSteam => (StorageModStateEnum.Created, repoModMatch, repoModVersion - 1),
                _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
            };
            var canWrite = action != ModActionEnum.UnusableSteam;

            mock.Setup(m => m.GetState()).Returns(state);
            mock.Setup(m => m.GetMatchHash()).Returns(GetMatchHash(match).Result);
            mock.Setup(m => m.GetVersionHash()).Returns(GetVersionHash(version).Result);
            mock.Setup(m => m.CanWrite).Returns(canWrite);

            var checkObj = new Mock<IModelRepositoryMod>(MockBehavior.Strict);
            checkObj.Setup(m => m.State).Returns(LoadingState.Loaded);
            checkObj.Setup(m => m.GetMatchHash()).Returns(GetMatchHash(repoModMatch).Result);
            checkObj.Setup(m => m.GetVersionHash()).Returns(GetVersionHash(repoModVersion).Result);

            Assert.Equal(action, CoreCalculation.GetModAction(checkObj.Object, mock.Object));

            return mock.Object;
        }
    }
}
