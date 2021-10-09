using System.Collections.Generic;
using System.Threading;
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

        private (ModActionEnum action, bool requiresMatchHash, bool requireVersionHash) DoCheck(int repoHash, int repoVersion, int? storageHash, int? storageVersion,  StorageModStateEnum state, bool canWrite = true)
        {
            // TODO: restructure a bit for less parameter passing all over the place
            var repoMod = new MockModelRepositoryMod(repoHash, repoVersion);
            var storageMod = new MockModelStorageMod(storageHash, storageVersion, state)
            {
                CanWrite = canWrite
            };

            var result = CoreCalculation.GetModAction(repoMod, storageMod, CancellationToken.None).Result;
            return (result, storageMod.RequiredMatchHash, storageMod.RequiredVersionHash);
        }


        [Fact]
        private void NoMatch()
        {
            var (action, requiresMatchHash, requireVersionHash) = DoCheck(1, 1, 2, null, StorageModStateEnum.Created);

            Assert.Equal(ModActionEnum.Unusable, action);
            Assert.True(requiresMatchHash);
            Assert.False(requireVersionHash);
        }

        [Fact]
        private void Use()
        {
            var (action, requiresMatchHash, requireVersionHash) = DoCheck(1, 1, 1, 1, StorageModStateEnum.Created);

            Assert.Equal(ModActionEnum.Use, action);
            Assert.True(requiresMatchHash);
            Assert.True(requireVersionHash);
        }

        [Fact]
        private void Update()
        {
            var (action, requiresMatchHash, requireVersionHash) = DoCheck(1, 1, 1, 2, StorageModStateEnum.Created);

            Assert.Equal(ModActionEnum.Update, action);
            Assert.True(requiresMatchHash);
            Assert.True(requireVersionHash);
        }

        [Fact]
        private void Error()
        {
            var (action, requiresMatchHash, requireVersionHash) = DoCheck(1, 1, null, null, StorageModStateEnum.Error);

            Assert.Equal(ModActionEnum.Unusable, action);
            Assert.False(requiresMatchHash);
            Assert.False(requireVersionHash);
        }

        [Fact]
        private void ContinueUpdate()
        {
            var (action, requiresMatchHash, requireVersionHash) = DoCheck(1, 1, 1, 1, StorageModStateEnum.CreatedWithUpdateTarget);

            Assert.Equal(ModActionEnum.ContinueUpdate, action);
            Assert.False(requiresMatchHash);
            Assert.True(requireVersionHash);
        }

        [Fact]
        private void AbortAndUpdate()
        {
            var (action, requiresMatchHash, requireVersionHash) = DoCheck(2, 1, 1, 1, StorageModStateEnum.CreatedWithUpdateTarget);

            Assert.Equal(ModActionEnum.ContinueUpdate, action);
            Assert.False(requiresMatchHash);
            Assert.True(requireVersionHash);
        }

        [Fact]
        private void Await()
        {
            var (action, requiresMatchHash, requireVersionHash) = DoCheck(1, 1, 1, 1, StorageModStateEnum.Updating);

            Assert.Equal(ModActionEnum.Await, action);
            Assert.False(requiresMatchHash);
            Assert.True(requireVersionHash);
        }

        [Fact]
        private void AbortActiveAndUpdate()
        {
            var (action, requiresMatchHash, requireVersionHash) = DoCheck(1, 1, 1, 2, StorageModStateEnum.Updating);

            Assert.Equal(ModActionEnum.AbortActiveAndUpdate, action);
            Assert.True(requiresMatchHash);
            Assert.True(requireVersionHash);
        }

        [Fact]
        private void DontUpdateSteam()
        {
            var (action, _, _) = DoCheck(1, 1, 1, 2, StorageModStateEnum.Created, false);

            Assert.Equal(ModActionEnum.Unusable, action);
        }
    }
}
