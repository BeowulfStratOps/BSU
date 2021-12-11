using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.Core.Services;
using BSU.Core.Tests.Util;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests.CoreCalculationTests
{
    public class CalculatedRepoitoryStateTests : LoggedTest
    {
        public CalculatedRepoitoryStateTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        private IModelRepositoryMod None()
        {
            var mock = new Mock<IModelRepositoryMod>(MockBehavior.Strict);
            mock.Setup(m => m.GetCurrentSelection()).Returns(new ModSelectionNone());
            return mock.Object;
        }

        private IModelRepositoryMod Loading()
        {
            var mock = new Mock<IModelRepositoryMod>(MockBehavior.Strict);
            mock.Setup(m => m.GetCurrentSelection()).Returns(new ModSelectionLoading());
            return mock.Object;
        }

        private IModelRepositoryMod Disabled()
        {
            var mock = new Mock<IModelRepositoryMod>(MockBehavior.Strict);
            mock.Setup(o => o.GetCurrentSelection()).Returns(new ModSelectionDisabled());
            return mock.Object;
        }

        private IModelRepositoryMod Download()
        {
            var storage = new Mock<IModelStorage>(MockBehavior.Strict);
            storage.Setup(s => s.State).Returns(LoadingState.Loaded);
            storage.Setup(s => s.HasMod("asdf")).Returns(false);
            var selection = new ModSelectionDownload(storage.Object);
            var mock = new Mock<IModelRepositoryMod>(MockBehavior.Strict);
            mock.Setup(o => o.GetCurrentSelection()).Returns(selection);
            mock.Setup(o => o.DownloadIdentifier).Returns("asdf");
            return mock.Object;
        }

        private IModelRepositoryMod StorageMod(ModActionEnum action)
        {
            var storageMod = AutoselectTests.FromAction(action);
            var selection = new ModSelectionStorageMod(storageMod);
            var mock = new Mock<IModelRepositoryMod>(MockBehavior.Strict);
            mock.Setup(m => m.GetCurrentSelection()).Returns(selection);
            mock.Setup(m => m.State).Returns(LoadingState.Loaded);
            var matchHash = TestUtils.GetMatchHash(1).Result;
            var versionHash = TestUtils.GetVersionHash(1).Result;
            mock.Setup(m => m.GetMatchHash()).Returns(matchHash);
            mock.Setup(m => m.GetVersionHash()).Returns(versionHash);
            return mock.Object;
        }

        // TODO: test with errors / loading / conflicts
        private CalculatedRepositoryStateEnum CalculateState(params IModelRepositoryMod[] mods)
        {
            var repo = new Mock<IModelRepository>(MockBehavior.Strict);
            repo.Setup(r => r.State).Returns(LoadingState.Loaded);
            repo.Setup(r => r.GetMods()).Returns(mods.ToList());
            return CoreCalculation.GetRepositoryState(repo.Object, mods.ToList());
        }

        [Fact]
        private void Single_Download()
        {
            var result = CalculateState(
                Download()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result);
        }

        [Fact]
        private void Single_Ready()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.Use)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.Ready, result);
        }

        [Fact]
        private void Single_Update()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.Update)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result);
        }

        [Fact]
        private void Single_UserIntervention()
        {
            var result = CalculateState(
                None()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.RequiresUserIntervention, result);
        }

        [Fact]
        private void Single_Await()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.Await)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.Syncing, result);
        }

        [Fact]
        private void Single_ContinueUpdate()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.ContinueUpdate)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result);
        }

        [Fact]
        private void Single_AbortAndUpdate()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.AbortAndUpdate)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result);
        }

        [Fact]
        private void UseAnd_Download()
        {
            var result = CalculateState(
                Download(),
                StorageMod(ModActionEnum.Use)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result);
        }

        [Fact]
        private void UseAnd_Ready()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.Use),
                StorageMod(ModActionEnum.Use)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.Ready, result);
        }

        [Fact]
        private void UseAnd_Update()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.Update),
                StorageMod(ModActionEnum.Use)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result);
        }

        [Fact]
        private void UseAnd_UserIntervention()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.Use),
                None()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.RequiresUserIntervention, result);
        }

        [Fact]
        private void UseAnd_Await()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.Use),
                StorageMod(ModActionEnum.Await)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.Syncing, result);
        }

        [Fact]
        private void UseAnd_ContinueUpdate()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.ContinueUpdate),
                StorageMod(ModActionEnum.Use)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result);
        }

        [Fact]
        private void UseAnd_AbortAndUpdate()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.AbortAndUpdate),
                StorageMod(ModActionEnum.Use)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result);
        }

        [Fact]
        private void UserIntervention_AndUpdate()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.Update),
                None()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.RequiresUserIntervention, result);
        }

        [Fact]
        private void MoreDownload()
        {
            var result = CalculateState(
                Download(),
                Download(),
                StorageMod(ModActionEnum.Update)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result);
        }

        [Fact]
        private void MoreUpdate()
        {
            var result = CalculateState(
                Download(),
                StorageMod(ModActionEnum.Update),
                StorageMod(ModActionEnum.Update)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result);
        }

        [Fact]
        private void DisabledAnd_Download()
        {
            var result = CalculateState(
                Download(),
                Disabled()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result);
        }

        [Fact]
        private void DisabledAnd_Ready()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.Use),
                Disabled()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.ReadyPartial, result);
        }

        [Fact]
        private void DisabledAnd_Update()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.Update),
                Disabled()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result);
        }

        [Fact]
        private void DisabledAnd_UserIntervention()
        {
            var result = CalculateState(
                None(),
                Disabled()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.RequiresUserIntervention, result);
        }

        [Fact]
        private void DisabledAnd_Await()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.Await),
                Disabled()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.Syncing, result);
        }

        [Fact]
        private void DisabledAnd_ContinueUpdate()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.ContinueUpdate),
                Disabled()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result);
        }

        [Fact]
        private void DisabledAnd_AbortAndUpdate()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.AbortAndUpdate),
                Disabled()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result);
        }

        [Fact]
        private void Single_Disabled()
        {
            var result = CalculateState(
                Disabled()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.ReadyPartial, result);
        }
    }
}
