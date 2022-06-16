using System.Collections.Generic;
using BSU.Core.Model;
using BSU.Core.Services;
using BSU.Core.Tests.Util;
using BSU.CoreCommon.Hashes;
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

        private ModActionEnum DoCheck(int repoHash, int repoVersion, int? storageHash, int? storageVersion,  StorageModStateEnum state, bool canWrite = true)
        {
            var repoMod = new Mock<IModelRepositoryMod>(MockBehavior.Strict);
            repoMod.SetupHashes(new TestMatchHash(repoHash), new TestVersionHash(repoVersion));
            repoMod.Setup(m => m.State).Returns(LoadingState.Loaded);

            var storageMod = new Mock<IModelStorageMod>(MockBehavior.Strict);
            var storageHashes = new List<IModHash>();
            if (storageHash != null)
                storageHashes.Add(new TestMatchHash((int)storageHash));
            if (storageVersion != null)
                storageHashes.Add(new TestVersionHash((int)storageVersion));
            storageMod.SetupHashes(storageHashes.ToArray());
            storageMod.Setup(m => m.GetState()).Returns(state);
            storageMod.Setup(m => m.CanWrite).Returns(canWrite);

            return new ModActionService().GetModAction(repoMod.Object, storageMod.Object);
        }


        [Fact]
        private void NoMatch()
        {
            var action = DoCheck(1, 1, 2, null, StorageModStateEnum.Created);

            Assert.Equal(ModActionEnum.Unusable, action);
        }

        [Fact]
        private void Use()
        {
            var action = DoCheck(1, 1, 1, 1, StorageModStateEnum.Created);

            Assert.Equal(ModActionEnum.Use, action);
        }

        [Fact]
        private void Update()
        {
            var action = DoCheck(1, 1, 1, 2, StorageModStateEnum.Created);

            Assert.Equal(ModActionEnum.Update, action);
        }

        [Fact]
        private void Error()
        {
            var action = DoCheck(1, 1, null, null, StorageModStateEnum.Error);

            Assert.Equal(ModActionEnum.Unusable, action);
        }

        [Fact]
        private void ContinueUpdate()
        {
            var action = DoCheck(1, 1, 1, 1, StorageModStateEnum.CreatedWithUpdateTarget);

            Assert.Equal(ModActionEnum.ContinueUpdate, action);
        }

        [Fact]
        private void AbortAndUpdate()
        {
            var action = DoCheck(2, 1, 1, 1, StorageModStateEnum.CreatedWithUpdateTarget);

            Assert.Equal(ModActionEnum.ContinueUpdate, action);
        }

        [Fact]
        private void Await()
        {
            var action = DoCheck(1, 1, 1, 1, StorageModStateEnum.Updating);

            Assert.Equal(ModActionEnum.Await, action);
        }

        [Fact]
        private void AbortActiveAndUpdate()
        {
            var action = DoCheck(1, 1, 1, 2, StorageModStateEnum.Updating);

            Assert.Equal(ModActionEnum.AbortActiveAndUpdate, action);
        }

        [Fact]
        private void DontUpdateSteam()
        {
            var action = DoCheck(1, 1, 1, 2, StorageModStateEnum.Created, false);

            Assert.Equal(ModActionEnum.UnusableSteam, action);
        }
    }
}
