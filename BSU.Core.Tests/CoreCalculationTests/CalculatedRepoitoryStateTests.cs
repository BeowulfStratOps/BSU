using System.Linq;
using System.Threading.Tasks;
using BSU.Core.Model;
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

        private (RepositoryModActionSelection, ModActionEnum?, bool) Null()
        {
            return (null, null, false);
        }

        private (RepositoryModActionSelection, ModActionEnum?, bool) DoNothing()
        {
            return (new RepositoryModActionDoNothing(), null, false);
        }

        private (RepositoryModActionSelection, ModActionEnum?, bool) Download()
        {
            var storage = new Mock<IModelStorage>(MockBehavior.Strict).Object;
            return (new RepositoryModActionDownload(storage), null, false);
        }

        private async Task<(RepositoryModActionSelection, ModActionEnum?, bool)> StorageMod(ModActionEnum action)
        {
            var storageMod = await AutoselectTests.FromAction(action);
            return (new RepositoryModActionStorageMod(storageMod), action, false);
        }

        private CalculatedRepositoryState CalculateState(
            params (RepositoryModActionSelection, ModActionEnum?, bool)[] modData)
        {
            return CoreCalculation.CalculateRepositoryState(modData.ToList());
        }

        [Fact]
        private void Single_Download()
        {
            var result = CalculateState(
                Download()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result.State);
        }

        [Fact]
        private async Task Single_Ready()
        {
            var result = CalculateState(
                await StorageMod(ModActionEnum.Use)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.Ready, result.State);
        }

        [Fact]
        private async Task Single_Update()
        {
            var result = CalculateState(
                await StorageMod(ModActionEnum.Update)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result.State);
        }

        [Fact]
        private void Single_UserIntervention()
        {
            var result = CalculateState(
                Null()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.RequiresUserIntervention, result.State);
        }

        [Fact]
        private async Task Single_Await()
        {
            var result = CalculateState(
                await StorageMod(ModActionEnum.Await)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.Syncing, result.State);
        }

        [Fact]
        private async Task Single_ContinueUpdate()
        {
            var result = CalculateState(
                await StorageMod(ModActionEnum.ContinueUpdate)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result.State);
        }

        [Fact]
        private async Task Single_AbortAndUpdate()
        {
            var result = CalculateState(
                await StorageMod(ModActionEnum.AbortAndUpdate)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result.State);
        }

        [Fact]
        private async Task UseAnd_Download()
        {
            var result = CalculateState(
                Download(),
                await StorageMod(ModActionEnum.Use)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result.State);
        }

        [Fact]
        private async Task UseAnd_Ready()
        {
            var result = CalculateState(
                await StorageMod(ModActionEnum.Use),
                await StorageMod(ModActionEnum.Use)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.Ready, result.State);
        }

        [Fact]
        private async Task UseAnd_Update()
        {
            var result = CalculateState(
                await StorageMod(ModActionEnum.Update),
                await StorageMod(ModActionEnum.Use)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result.State);
        }

        [Fact]
        private async Task UseAnd_UserIntervention()
        {
            var result = CalculateState(
                await StorageMod(ModActionEnum.Use),
                Null()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.RequiresUserIntervention, result.State);
        }

        [Fact]
        private async Task UseAnd_Await()
        {
            var result = CalculateState(
                await StorageMod(ModActionEnum.Use),
                await StorageMod(ModActionEnum.Await)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.Syncing, result.State);
        }

        [Fact]
        private async Task UseAnd_ContinueUpdate()
        {
            var result = CalculateState(
                await StorageMod(ModActionEnum.ContinueUpdate),
                await StorageMod(ModActionEnum.Use)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result.State);
        }

        [Fact]
        private async Task UseAnd_AbortAndUpdate()
        {
            var result = CalculateState(
                await StorageMod(ModActionEnum.AbortAndUpdate),
                await StorageMod(ModActionEnum.Use)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result.State);
        }

        [Fact]
        private async Task UserIntervention_AndUpdate()
        {
            var result = CalculateState(
                await StorageMod(ModActionEnum.Update),
                Null()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.RequiresUserIntervention, result.State);
        }

        [Fact]
        private async Task MoreDownload()
        {
            var result = CalculateState(
                Download(),
                Download(),
                await StorageMod(ModActionEnum.Update)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result.State);
        }

        [Fact]
        private async Task MoreUpdate()
        {
            var result = CalculateState(
                Download(),
                await StorageMod(ModActionEnum.Update),
                await StorageMod(ModActionEnum.Update)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result.State);
        }

        [Fact]
        private void DoNothingAnd_Download()
        {
            var result = CalculateState(
                Download(),
                DoNothing()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result.State);
        }

        [Fact]
        private async Task DoNothingAnd_Ready()
        {
            var result = CalculateState(
                await StorageMod(ModActionEnum.Use),
                DoNothing()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.ReadyPartial, result.State);
        }

        [Fact]
        private async Task DoNothingAnd_Update()
        {
            var result = CalculateState(
                await StorageMod(ModActionEnum.Update),
                DoNothing()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result.State);
        }

        [Fact]
        private void DoNothingAnd_UserIntervention()
        {
            var result = CalculateState(
                Null(),
                DoNothing()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.RequiresUserIntervention, result.State);
        }

        [Fact]
        private async Task DoNothingAnd_Await()
        {
            var result = CalculateState(
                await StorageMod(ModActionEnum.Await),
                DoNothing()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.Syncing, result.State);
        }

        [Fact]
        private async Task DoNothingAnd_ContinueUpdate()
        {
            var result = CalculateState(
                await StorageMod(ModActionEnum.ContinueUpdate),
                DoNothing()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result.State);
        }

        [Fact]
        private async Task DoNothingAnd_AbortAndUpdate()
        {
            var result = CalculateState(
                await StorageMod(ModActionEnum.AbortAndUpdate),
                DoNothing()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result.State);
        }

        [Fact]
        private void Single_DoNothing()
        {
            var result = CalculateState(
                DoNothing()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.ReadyPartial, result.State);
        }
    }
}
