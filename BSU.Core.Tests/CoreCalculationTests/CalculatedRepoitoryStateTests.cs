using System;
using System.Collections.Generic;
using BSU.Core.Model;
using BSU.Core.Tests.Mocks;
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

        private IModelRepositoryMod CreateMod(MockModelStructure structure, ModActionEnum? selectedModAction,
            bool hasDownloadSelected, bool doNothing = false)
        {
            if (selectedModAction != null && hasDownloadSelected) throw new ArgumentException();

            RepositoryModActionSelection selection = null;

            if (selectedModAction != null)
            {
                var storageMod = AutoselectTests.AddAction(structure, (ModActionEnum) selectedModAction);
                selection = new RepositoryModActionSelection(storageMod);
            }

            if (hasDownloadSelected)
            {
                var download = new Mock<IModelStorage>(MockBehavior.Strict);
                selection = new RepositoryModActionSelection(download.Object);
            }

            if (doNothing)
            {
                selection = new RepositoryModActionSelection();
            }

            var res = new MockModelRepositoryMod(1, 1) {Selection = selection};

            return res;
        }

        [Fact]
        private void Single_Download()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, null, true)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsDownload, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void Single_Ready()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, ModActionEnum.Use, false)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.Ready, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void Single_Update()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, ModActionEnum.Update, false)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsUpdate, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void Single_Loading()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, ModActionEnum.Loading, false)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.Loading, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void Single_UserIntervention()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, null, false)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.RequiresUserIntervention, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void Single_Await()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, ModActionEnum.Await, false)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.InProgress, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void Single_ContinueUpdate()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, ModActionEnum.ContinueUpdate, false)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsUpdate, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void Single_AbortAndUpdate()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, ModActionEnum.AbortAndUpdate, false)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsUpdate, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void UseAnd_Download()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, null, true),
                CreateMod(structure, ModActionEnum.Use, false)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsDownload, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void UseAnd_Ready()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, ModActionEnum.Use, false),
                CreateMod(structure, ModActionEnum.Use, false)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.Ready, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void UseAnd_Update()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, ModActionEnum.Update, false),
                CreateMod(structure, ModActionEnum.Use, false)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsUpdate, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void UseAnd_Loading()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, ModActionEnum.Loading, false),
                CreateMod(structure, ModActionEnum.Use, false)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.Loading, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void UseAnd_UserIntervention()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, null, false),
                CreateMod(structure, ModActionEnum.Use, false)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.RequiresUserIntervention, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void UseAnd_Await()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, ModActionEnum.Await, false),
                CreateMod(structure, ModActionEnum.Use, false)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.InProgress, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void UseAnd_ContinueUpdate()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, ModActionEnum.ContinueUpdate, false),
                CreateMod(structure, ModActionEnum.Use, false)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsUpdate, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void UseAnd_AbortAndUpdate()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, ModActionEnum.AbortAndUpdate, false),
                CreateMod(structure, ModActionEnum.Use, false)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsUpdate, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void UserIntervention_AndUpdate()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, null, false),
                CreateMod(structure, ModActionEnum.Update, false)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.RequiresUserIntervention, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void MoreDownload()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, null, true),
                CreateMod(structure, null, true),
                CreateMod(structure, ModActionEnum.Update, false)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsDownload, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void MoreUpdate()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, null, true),
                CreateMod(structure, ModActionEnum.Update, false),
                CreateMod(structure, ModActionEnum.Update, false)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsUpdate, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void DoNothingAnd_Download()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, null, true),
                CreateMod(structure, null, false, true)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsDownload, result.State);
            Assert.True(result.IsPartial);
        }

        [Fact]
        private void DoNothingAnd_Ready()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, ModActionEnum.Use, false),
                CreateMod(structure, null, false, true)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.Ready, result.State);
            Assert.True(result.IsPartial);
        }

        [Fact]
        private void DoNothingAnd_Update()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, ModActionEnum.Update, false),
                CreateMod(structure, null, false, true)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsUpdate, result.State);
            Assert.True(result.IsPartial);
        }

        [Fact]
        private void DoNothingAnd_Loading()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, ModActionEnum.Loading, false),
                CreateMod(structure, null, false, true)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.Loading, result.State);
            Assert.True(result.IsPartial);
        }

        [Fact]
        private void DoNothingAnd_UserIntervention()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, null, false),
                CreateMod(structure, null, false, true)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.RequiresUserIntervention, result.State);
            Assert.True(result.IsPartial);
        }

        [Fact]
        private void DoNothingAnd_Await()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, ModActionEnum.Await, false),
                CreateMod(structure, null, false, true)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.InProgress, result.State);
            Assert.True(result.IsPartial);
        }

        [Fact]
        private void DoNothingAnd_ContinueUpdate()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, ModActionEnum.ContinueUpdate, false),
                CreateMod(structure, null, false, true)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsUpdate, result.State);
            Assert.True(result.IsPartial);
        }

        [Fact]
        private void DoNothingAnd_AbortAndUpdate()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, ModActionEnum.AbortAndUpdate, false),
                CreateMod(structure, null, false, true)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsUpdate, result.State);
            Assert.True(result.IsPartial);
        }

        [Fact]
        private void Single_DoNothing()
        {
            var structure = new MockModelStructure();
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(structure, null, false, true)
            }, structure);
            Assert.Equal(CalculatedRepositoryStateEnum.Ready, result.State);
            Assert.True(result.IsPartial);
        }
    }
}
