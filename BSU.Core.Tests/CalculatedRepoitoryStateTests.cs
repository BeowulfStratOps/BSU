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

        private IModelRepositoryMod CreateMod(ModActionEnum? selectedModAction, bool hasDownloadSelected)
        {
            if (selectedModAction != null && hasDownloadSelected) throw new ArgumentException();
            
            var result = new Mock<IModelRepositoryMod>(MockBehavior.Strict);
            if (selectedModAction == null)
            {
                result.Setup(m => m.SelectedStorageMod).Returns((IModelStorageMod)null);
                result.Setup(m => m.Actions).Returns(new Dictionary<IModelStorageMod, ModAction>());
            }
            else
            {
                var storageMod = new Mock<IModelStorageMod>(MockBehavior.Strict);
                result.Setup(m => m.SelectedStorageMod).Returns(storageMod.Object);
                var actions = new Dictionary<IModelStorageMod, ModAction>
                {
                    {
                        storageMod.Object,
                        new ModAction((ModActionEnum) selectedModAction, null, null, new HashSet<ModAction>())
                    }
                };
                result.Setup(m => m.Actions).Returns(actions);
            }

            if (hasDownloadSelected)
            {
                var download = new Mock<IModelStorage>(MockBehavior.Strict);
                result.Setup(m => m.SelectedDownloadStorage).Returns(download.Object);
            }
            else
            {
                result.Setup(m => m.SelectedDownloadStorage).Returns((IModelStorage) null);
            }

            return result.Object;
        }

        [Fact]
        private void Single_Download()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(null, true)
            });
            Assert.Equal(CalculatedRepositoryState.NeedsDownload, result);
        }
        
        [Fact]
        private void Single_Ready()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.Use, false)
            });
            Assert.Equal(CalculatedRepositoryState.Ready, result);
        }
        
        [Fact]
        private void Single_Update()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.Update, false)
            });
            Assert.Equal(CalculatedRepositoryState.NeedsUpdate, result);
        }
        
        [Fact]
        private void Single_Loading()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.Loading, false)
            });
            Assert.Equal(CalculatedRepositoryState.Loading, result);
        }
        
        [Fact]
        private void Single_UserIntervention()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(null, false)
            });
            Assert.Equal(CalculatedRepositoryState.RequiresUserIntervention, result);
        }
        
        [Fact]
        private void Single_Await()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.Await, false)
            });
            Assert.Equal(CalculatedRepositoryState.InProgress, result);
        }
        
        [Fact]
        private void Single_ContinueUpdate()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.ContinueUpdate, false)
            });
            Assert.Equal(CalculatedRepositoryState.NeedsUpdate, result);
        }
        
        [Fact]
        private void Single_AbortAndUpdate()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.AbortAndUpdate, false)
            });
            Assert.Equal(CalculatedRepositoryState.NeedsUpdate, result);
        }
        
        [Fact]
        private void UseAnd_Download()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(null, true),
                CreateMod(ModActionEnum.Use, false)
            });
            Assert.Equal(CalculatedRepositoryState.NeedsDownload, result);
        }
        
        [Fact]
        private void UseAnd_Ready()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.Use, false),
                CreateMod(ModActionEnum.Use, false)
            });
            Assert.Equal(CalculatedRepositoryState.Ready, result);
        }
        
        [Fact]
        private void UseAnd_Update()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.Update, false),
                CreateMod(ModActionEnum.Use, false)
            });
            Assert.Equal(CalculatedRepositoryState.NeedsUpdate, result);
        }
        
        [Fact]
        private void UseAnd_Loading()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.Loading, false),
                CreateMod(ModActionEnum.Use, false)
            });
            Assert.Equal(CalculatedRepositoryState.Loading, result);
        }
        
        [Fact]
        private void UseAnd_UserIntervention()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(null, false),
                CreateMod(ModActionEnum.Use, false)
            });
            Assert.Equal(CalculatedRepositoryState.RequiresUserIntervention, result);
        }
        
        [Fact]
        private void UseAnd_Await()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.Await, false),
                CreateMod(ModActionEnum.Use, false)
            });
            Assert.Equal(CalculatedRepositoryState.InProgress, result);
        }
        
        [Fact]
        private void UseAnd_ContinueUpdate()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.ContinueUpdate, false),
                CreateMod(ModActionEnum.Use, false)
            });
            Assert.Equal(CalculatedRepositoryState.NeedsUpdate, result);
        }
        
        [Fact]
        private void UseAnd_AbortAndUpdate()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(ModActionEnum.AbortAndUpdate, false),
                CreateMod(ModActionEnum.Use, false)
            });
            Assert.Equal(CalculatedRepositoryState.NeedsUpdate, result);
        }
        
        [Fact]
        private void UserIntervention_AndUpdate()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                CreateMod(null, false),
                CreateMod(ModActionEnum.Update, false)
            });
            Assert.Equal(CalculatedRepositoryState.RequiresUserIntervention, result);
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
            Assert.Equal(CalculatedRepositoryState.NeedsDownload, result);
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
            Assert.Equal(CalculatedRepositoryState.NeedsUpdate, result);
        }
    }
}