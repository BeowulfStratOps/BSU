using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.Core.Tests.Util;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests.CoreCalculationTests
{
    public class AutoselectTests : LoggedTest
    {
        public AutoselectTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        internal static async Task<IModelStorageMod> FromAction(ModActionEnum actionType, bool canWrite = true)
        {
            var storageMod = actionType switch
            {
                ModActionEnum.Update => new MockModelStorageMod(1, 2, StorageModStateEnum.Created),
                ModActionEnum.ContinueUpdate => new MockModelStorageMod(1, 1, StorageModStateEnum.CreatedWithUpdateTarget),
                ModActionEnum.Await => new MockModelStorageMod(1, 1, StorageModStateEnum.Updating),
                ModActionEnum.Use => new MockModelStorageMod(1, 1, StorageModStateEnum.Created),
                ModActionEnum.AbortAndUpdate => new MockModelStorageMod(1, 2, StorageModStateEnum.CreatedWithUpdateTarget),
                ModActionEnum.AbortActiveAndUpdate => new MockModelStorageMod(1, 2, StorageModStateEnum.Updating),
                ModActionEnum.Unusable => new MockModelStorageMod(2, 2, StorageModStateEnum.Created),
                _ => throw new ArgumentOutOfRangeException(nameof(actionType), actionType, null)
            };
            storageMod.CanWrite = canWrite;

            // just checking that we got the setup right
            var testRepoMod = new MockModelRepositoryMod(1, 1);
            Assert.Equal(actionType, await CoreCalculation.GetModAction(testRepoMod, storageMod, CancellationToken.None));

            return storageMod;
        }
        private async Task<IModelStorageMod> AutoSelect(IModelRepositoryMod repoMod,
            params IModelStorageMod[] storageMods)
        {
            return await CoreCalculation.AutoSelect(repoMod, storageMods.ToList(), CancellationToken.None);
        }

        [Fact]
        private async Task SingleUse_AllLoaded_NoConflict()
        {
            var repoMod = new MockModelRepositoryMod(1, 1);
            var storageMod = await FromAction(ModActionEnum.Use);

            var mod = await AutoSelect(repoMod, storageMod);

            Assert.Equal(storageMod, mod);
        }

        [Fact]
        private async Task SingleUse_AllLoaded_Conflict()
        {
            var repoMod = new MockModelRepositoryMod(1, 1);
            var storageMod = await FromAction(ModActionEnum.Use);
            repoMod.Conflicts[storageMod] = new List<IModelRepositoryMod> { new MockModelRepositoryMod(1, 3) }; // TODO: this whole conflict thing is kinda messy..

            var result = await AutoSelect(repoMod, storageMod);

            Assert.Null(result);
        }

        [Fact]
        private async Task Precedence()
        {
            var repoMod = new MockModelRepositoryMod(1, 1);
            var storageMod = await FromAction(ModActionEnum.Use);
            var storageMod2 = await FromAction(ModActionEnum.Update);

            var mod = await AutoSelect(repoMod, storageMod, storageMod2);

            Assert.Equal(storageMod, mod);
        }

        [Fact]
        private async Task PreferNonSteam()
        {
            var repoMod = new MockModelRepositoryMod(1, 1);
            var storageMod = await FromAction(ModActionEnum.Use, false);
            var storageMod2 = await FromAction(ModActionEnum.Use);

            var mod = await AutoSelect(repoMod, storageMod, storageMod2);

            Assert.Equal(storageMod2, mod);
        }
    }
}
