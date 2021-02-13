using System;
using System.Collections.Generic;
using BSU.Core.Model;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests
{
    public class CalculatedRepoitoryStateTests : LoggedTest
    {
        public CalculatedRepoitoryStateTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        private IModelRepositoryMod CreateMod(ModActionEnum? selectedModAction, bool hasDownloadSelected, bool doNothing = false)
        {
            if (selectedModAction != null && hasDownloadSelected) throw new ArgumentException();

            var result = new Mock<IModelRepositoryMod>(MockBehavior.Strict);
            if (selectedModAction != null)
            {
                var storageMod = new Mock<IModelStorageMod>(MockBehavior.Strict);
                var selection = new RepositoryModActionSelection(storageMod.Object);
                result.Setup(m => m.Selection).Returns(selection);
                var actions = new Dictionary<IModelStorageMod, ModAction>
                {
                    {
                        storageMod.Object,
                        new ModAction((ModActionEnum) selectedModAction, null, null, new HashSet<ModAction>())
                    }
                };
                result.Setup(m => m.LocalMods).Returns(actions);
                return result.Object;
            }

            if (hasDownloadSelected)
            {
                var download = new Mock<IModelStorage>(MockBehavior.Strict);
                var selection = new RepositoryModActionSelection(download.Object);
                result.Setup(m => m.Selection).Returns(selection);
                result.Setup(m => m.LocalMods).Returns(new Dictionary<IModelStorageMod, ModAction>());
                return result.Object;
            }

            if (doNothing)
            {
                var selection = new RepositoryModActionSelection();
                result.Setup(m => m.Selection).Returns(selection);
                result.Setup(m => m.LocalMods).Returns(new Dictionary<IModelStorageMod, ModAction>());
                return result.Object;
            }

            result.Setup(m => m.LocalMods).Returns(new Dictionary<IModelStorageMod, ModAction>());
            result.Setup(m => m.Selection).Returns((RepositoryModActionSelection)null);

            return result.Object;
        }

        [Fact]
        private void Single_Download()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(null, true)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsDownload, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void Single_Ready()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.Use, false)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.Ready, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void Single_Update()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.Update, false)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsUpdate, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void Single_Loading()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.Loading, false)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.Loading, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void Single_UserIntervention()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(null, false)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.RequiresUserIntervention, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void Single_Await()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.Await, false)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.InProgress, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void Single_ContinueUpdate()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.ContinueUpdate, false)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsUpdate, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void Single_AbortAndUpdate()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.AbortAndUpdate, false)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsUpdate, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void UseAnd_Download()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(null, true),
                CreateMod(ModActionEnum.Use, false)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsDownload, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void UseAnd_Ready()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.Use, false),
                CreateMod(ModActionEnum.Use, false)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.Ready, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void UseAnd_Update()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.Update, false),
                CreateMod(ModActionEnum.Use, false)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsUpdate, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void UseAnd_Loading()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.Loading, false),
                CreateMod(ModActionEnum.Use, false)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.Loading, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void UseAnd_UserIntervention()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(null, false),
                CreateMod(ModActionEnum.Use, false)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.RequiresUserIntervention, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void UseAnd_Await()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.Await, false),
                CreateMod(ModActionEnum.Use, false)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.InProgress, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void UseAnd_ContinueUpdate()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.ContinueUpdate, false),
                CreateMod(ModActionEnum.Use, false)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsUpdate, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void UseAnd_AbortAndUpdate()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.AbortAndUpdate, false),
                CreateMod(ModActionEnum.Use, false)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsUpdate, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void UserIntervention_AndUpdate()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(null, false),
                CreateMod(ModActionEnum.Update, false)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.RequiresUserIntervention, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void MoreDownload()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(null, true),
                CreateMod(null, true),
                CreateMod(ModActionEnum.Update, false)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsDownload, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void MoreUpdate()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(null, true),
                CreateMod(ModActionEnum.Update, false),
                CreateMod(ModActionEnum.Update, false)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsUpdate, result.State);
            Assert.False(result.IsPartial);
        }

        [Fact]
        private void DoNothingAnd_Download()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(null, true),
                CreateMod(null, false, true)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsDownload, result.State);
            Assert.True(result.IsPartial);
        }

        [Fact]
        private void DoNothingAnd_Ready()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.Use, false),
                CreateMod(null, false, true)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.Ready, result.State);
            Assert.True(result.IsPartial);
        }

        [Fact]
        private void DoNothingAnd_Update()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.Update, false),
                CreateMod(null, false, true)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsUpdate, result.State);
            Assert.True(result.IsPartial);
        }

        [Fact]
        private void DoNothingAnd_Loading()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.Loading, false),
                CreateMod(null, false, true)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.Loading, result.State);
            Assert.True(result.IsPartial);
        }

        [Fact]
        private void DoNothingAnd_UserIntervention()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(null, false),
                CreateMod(null, false, true)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.RequiresUserIntervention, result.State);
            Assert.True(result.IsPartial);
        }

        [Fact]
        private void DoNothingAnd_Await()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.Await, false),
                CreateMod(null, false, true)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.InProgress, result.State);
            Assert.True(result.IsPartial);
        }

        [Fact]
        private void DoNothingAnd_ContinueUpdate()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.ContinueUpdate, false),
                CreateMod(null, false, true)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsUpdate, result.State);
            Assert.True(result.IsPartial);
        }

        [Fact]
        private void DoNothingAnd_AbortAndUpdate()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.AbortAndUpdate, false),
                CreateMod(null, false, true)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.NeedsUpdate, result.State);
            Assert.True(result.IsPartial);
        }

        [Fact]
        private void Single_DoNothing()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(null, false, true)
            });
            Assert.Equal(CalculatedRepositoryStateEnum.Ready, result.State);
            Assert.True(result.IsPartial);
        }
    }
}
