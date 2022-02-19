using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.Model;
using BSU.Core.Services;
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

        internal static IModelStorageMod FromAction(ModActionEnum actionType, bool canWrite = true)
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
            Assert.Equal(actionType, CoreCalculation.GetModAction(testRepoMod, storageMod));

            return storageMod;
        }
        private IModelStorageMod? AutoSelect(IModelRepositoryMod repoMod, IEnumerable<IModelRepositoryMod>? allRepoMods = null,
            params IModelStorageMod[] storageMods)
        {
            return CoreCalculation.AutoSelect(repoMod, storageMods.ToList(), allRepoMods?.ToList() ?? new List<IModelRepositoryMod>());
        }

        [Fact]
        private void SingleUse_AllLoaded_NoConflict()
        {
            var repoMod = new MockModelRepositoryMod(1, 1);
            var storageMod = FromAction(ModActionEnum.Use);

            var mod = AutoSelect(repoMod, null, storageMod);

            Assert.Equal(storageMod, mod);
        }

        [Fact]
        private void SingleUse_AllLoaded_Conflict()
        {
            var repoMod = new MockModelRepositoryMod(1, 1);
            var storageMod = FromAction(ModActionEnum.Use);

            var conflict = new MockModelRepositoryMod(1, 3);
            conflict.SetSelection(new ModSelectionStorageMod(storageMod));

            var result = AutoSelect(repoMod, new[] { conflict }, storageMod);

            Assert.Null(result);
        }

        [Fact]
        private void Precedence()
        {
            var repoMod = new MockModelRepositoryMod(1, 1);
            var storageMod = FromAction(ModActionEnum.Use);
            var storageMod2 = FromAction(ModActionEnum.Update);

            var mod = AutoSelect(repoMod, null, storageMod, storageMod2);

            Assert.Equal(storageMod, mod);
        }

        [Fact]
        private void PreferNonSteam()
        {
            var repoMod = new MockModelRepositoryMod(1, 1);
            var storageMod = FromAction(ModActionEnum.Use, false);
            var storageMod2 = FromAction(ModActionEnum.Use);

            var mod = AutoSelect(repoMod, null, storageMod, storageMod2);

            Assert.Equal(storageMod2, mod);
        }
    }
}
