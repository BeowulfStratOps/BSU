using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BSU.Core.Model;
using BSU.Core.Tests.Mocks;
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

        internal static IModelStorageMod FromAction(ModActionEnum actionType)
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

            // just checking that we got the setup right
            var testRepoMod = new MockModelRepositoryMod(1, 1);
            Assert.Equal(actionType, CoreCalculation.GetModAction(testRepoMod, storageMod, CancellationToken.None).Result);

            return storageMod;
        }
        private (AutoSelectResult result, IModelStorageMod mod) AutoSelect(IModelRepositoryMod repoMod,
            params IModelStorageMod[] storageMods)
        {
            return CoreCalculation.AutoSelect(repoMod, storageMods.ToList(), CancellationToken.None).Result;
        }

        [Fact]
        private void SingleUse_AllLoaded_NoConflict()
        {
            var repoMod = new MockModelRepositoryMod(1, 1);
            var storageMod = FromAction(ModActionEnum.Use);

            var (result, mod) = AutoSelect(repoMod, storageMod);

            Assert.Equal(AutoSelectResult.Success, result);
            Assert.Equal(storageMod, mod);
        }

        [Fact]
        private void SingleUse_AllLoaded_Conflict()
        {
            var repoMod = new MockModelRepositoryMod(1, 1);
            var storageMod = FromAction(ModActionEnum.Use);
            repoMod.Conflicts[storageMod] = new List<IModelRepositoryMod> { new MockModelRepositoryMod(1, 3) }; // TODO: this whole conflict thing is kinda messy..

            var (result, _) = AutoSelect(repoMod, storageMod);

            Assert.Equal(AutoSelectResult.None, result);
        }

        [Fact]
        private void Precedence()
        {
            var repoMod = new MockModelRepositoryMod(1, 1);
            var storageMod = FromAction(ModActionEnum.Use);
            var storageMod2 = FromAction(ModActionEnum.Update);

            var (result, mod) = AutoSelect(repoMod, storageMod);

            Assert.Equal(AutoSelectResult.Success, result);
            Assert.Equal(storageMod, mod);
        }
    }
}
