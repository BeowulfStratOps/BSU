using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.Core.Tests.Util;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests.CoreCalculationTests
{
    public class MatchMakerTests : LoggedTest
    {
        public MatchMakerTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        private async Task<(ModActionEnum action, bool requiresMatchHash, bool requireVersionHash)> DoCheck(int repoHash, int repoVersion, int? storageHash, int? storageVersion,  StorageModStateEnum state, bool canWrite = true)
        {
            // TODO: restructure a bit for less parameter passing all over the place
            var repoMod = new MockModelRepositoryMod(repoHash, repoVersion);
            var storageMod = new MockModelStorageMod(storageHash, storageVersion, state)
            {
                CanWrite = canWrite
            };

            var result = await CoreCalculation.GetModAction(repoMod, storageMod, CancellationToken.None);
            return (result, storageMod.RequiredMatchHash, storageMod.RequiredVersionHash);
        }


        [Fact]
        private async Task NoMatch()
        {
            var (action, requiresMatchHash, requireVersionHash) = await DoCheck(1, 1, 2, null, StorageModStateEnum.Created);

            Assert.Equal(ModActionEnum.Unusable, action);
            Assert.True(requiresMatchHash);
            Assert.False(requireVersionHash);
        }

        [Fact]
        private async Task Use()
        {
            var (action, requiresMatchHash, requireVersionHash) = await DoCheck(1, 1, 1, 1, StorageModStateEnum.Created);

            Assert.Equal(ModActionEnum.Use, action);
            Assert.True(requiresMatchHash);
            Assert.True(requireVersionHash);
        }

        [Fact]
        private async Task Update()
        {
            var (action, requiresMatchHash, requireVersionHash) = await DoCheck(1, 1, 1, 2, StorageModStateEnum.Created);

            Assert.Equal(ModActionEnum.Update, action);
            Assert.True(requiresMatchHash);
            Assert.True(requireVersionHash);
        }

        [Fact]
        private async Task Error()
        {
            var (action, requiresMatchHash, requireVersionHash) = await DoCheck(1, 1, null, null, StorageModStateEnum.Error);

            Assert.Equal(ModActionEnum.Unusable, action);
            Assert.False(requiresMatchHash);
            Assert.False(requireVersionHash);
        }

        [Fact]
        private async Task ContinueUpdate()
        {
            var (action, requiresMatchHash, requireVersionHash) = await DoCheck(1, 1, 1, 1, StorageModStateEnum.CreatedWithUpdateTarget);

            Assert.Equal(ModActionEnum.ContinueUpdate, action);
            Assert.False(requiresMatchHash);
            Assert.True(requireVersionHash);
        }

        [Fact]
        private async Task AbortAndUpdate()
        {
            var (action, requiresMatchHash, requireVersionHash) = await DoCheck(2, 1, 1, 1, StorageModStateEnum.CreatedWithUpdateTarget);

            Assert.Equal(ModActionEnum.ContinueUpdate, action);
            Assert.False(requiresMatchHash);
            Assert.True(requireVersionHash);
        }

        [Fact]
        private async Task Await()
        {
            var (action, requiresMatchHash, requireVersionHash) = await DoCheck(1, 1, 1, 1, StorageModStateEnum.Updating);

            Assert.Equal(ModActionEnum.Await, action);
            Assert.False(requiresMatchHash);
            Assert.True(requireVersionHash);
        }

        [Fact]
        private async Task AbortActiveAndUpdate()
        {
            var (action, requiresMatchHash, requireVersionHash) = await DoCheck(1, 1, 1, 2, StorageModStateEnum.Updating);

            Assert.Equal(ModActionEnum.AbortActiveAndUpdate, action);
            Assert.True(requiresMatchHash);
            Assert.True(requireVersionHash);
        }

        [Fact]
        private async Task DontUpdateSteam()
        {
            var (action, _, _) = await DoCheck(1, 1, 1, 2, StorageModStateEnum.Created, false);

            Assert.Equal(ModActionEnum.Unusable, action);
        }
    }
}
