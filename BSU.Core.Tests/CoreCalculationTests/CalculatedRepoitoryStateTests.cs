﻿using System.Linq;
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

        private (RepositoryModActionSelection, ModActionEnum?, bool) StorageMod(ModActionEnum? action)
        {
            var storageMod = AutoselectTests.FromAction((ModActionEnum)action);
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
        private void Single_Ready()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.Use)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.Ready, result.State);
        }

        [Fact]
        private void Single_Update()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.Update)
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
        private void Single_Await()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.Await)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.Syncing, result.State);
        }

        [Fact]
        private void Single_ContinueUpdate()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.ContinueUpdate)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result.State);
        }

        [Fact]
        private void Single_AbortAndUpdate()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.AbortAndUpdate)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result.State);
        }

        [Fact]
        private void UseAnd_Download()
        {
            var result = CalculateState(
                Download(),
                StorageMod(ModActionEnum.Use)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result.State);
        }

        [Fact]
        private void UseAnd_Ready()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.Use),
                StorageMod(ModActionEnum.Use)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.Ready, result.State);
        }

        [Fact]
        private void UseAnd_Update()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.Update),
                StorageMod(ModActionEnum.Use)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result.State);
        }

        [Fact]
        private void UseAnd_UserIntervention()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.Use),
                Null()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.RequiresUserIntervention, result.State);
        }

        [Fact]
        private void UseAnd_Await()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.Use),
                StorageMod(ModActionEnum.Await)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.Syncing, result.State);
        }

        [Fact]
        private void UseAnd_ContinueUpdate()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.ContinueUpdate),
                StorageMod(ModActionEnum.Use)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result.State);
        }

        [Fact]
        private void UseAnd_AbortAndUpdate()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.AbortAndUpdate),
                StorageMod(ModActionEnum.Use)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result.State);
        }

        [Fact]
        private void UserIntervention_AndUpdate()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.Update),
                Null()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.RequiresUserIntervention, result.State);
        }

        [Fact]
        private void MoreDownload()
        {
            var result = CalculateState(
                Download(),
                Download(),
                StorageMod(ModActionEnum.Update)
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result.State);
        }

        [Fact]
        private void MoreUpdate()
        {
            var result = CalculateState(
                Download(),
                StorageMod(ModActionEnum.Update),
                StorageMod(ModActionEnum.Update)
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
        private void DoNothingAnd_Ready()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.Use),
                DoNothing()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.ReadyPartial, result.State);
        }

        [Fact]
        private void DoNothingAnd_Update()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.Update),
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
        private void DoNothingAnd_Await()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.Await),
                DoNothing()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.Syncing, result.State);
        }

        [Fact]
        private void DoNothingAnd_ContinueUpdate()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.ContinueUpdate),
                DoNothing()
            );
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsSync, result.State);
        }

        [Fact]
        private void DoNothingAnd_AbortAndUpdate()
        {
            var result = CalculateState(
                StorageMod(ModActionEnum.AbortAndUpdate),
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
