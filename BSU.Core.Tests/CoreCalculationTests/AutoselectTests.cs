using System;
using System.Collections.Generic;
using BSU.Core.Model;
using BSU.Core.Persistence;
using BSU.Core.Tests.Mocks;
using BSU.Core.Tests.Util;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests.CoreCalculationTests
{
    public class AutoselectTests : LoggedTest
    {
        public AutoselectTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        internal static IModelStorageMod AddAction(MockModelStructure structure, ModActionEnum actionType)
        {
            var storageMod = actionType switch
            {
                ModActionEnum.Update => new MockModelStorageMod(1, 2, StorageModStateEnum.Versioned),
                ModActionEnum.ContinueUpdate => new MockModelStorageMod(1, 1, StorageModStateEnum.LoadedWithUpdateTarget),
                ModActionEnum.Await => new MockModelStorageMod(1, 1, StorageModStateEnum.Updating),
                ModActionEnum.Use => new MockModelStorageMod(1, 1, StorageModStateEnum.Versioned),
                ModActionEnum.AbortAndUpdate => new MockModelStorageMod(1, 2, StorageModStateEnum.Updating),
                ModActionEnum.Unusable => new MockModelStorageMod(2, 2, StorageModStateEnum.Versioned),
                ModActionEnum.Loading => new MockModelStorageMod(1, null, StorageModStateEnum.Matched),
                ModActionEnum.Error => new MockModelStorageMod(null, null, StorageModStateEnum.Error),
                ModActionEnum.LoadingMatch => new MockModelStorageMod(null, null, StorageModStateEnum.Created),
                _ => throw new ArgumentOutOfRangeException(nameof(actionType), actionType, null)
            };

            structure.StorageMods.Add(storageMod);

            // just checking that we got the setup right
            var testRepoMod = new MockModelRepositoryMod(1, 1);
            Assert.Equal(actionType, CoreCalculation.GetModAction(testRepoMod, storageMod));

            return storageMod;
        }

        private void AddConflict(MockModelStructure structure)
        {
            structure.RepositoryMods.Add(new MockModelRepositoryMod(1, 3));
        }

        private (IModelRepositoryMod repoMod, MockModelStructure structure) CommonSetup()
        {
            var repoMod = new MockModelRepositoryMod(1, 1);
            var structure = new MockModelStructure {RepositoryMods = {repoMod}};
            return (repoMod, structure);
        }

        [Fact]
        private void SingleUse_AllLoaded_NoConflict()
        {
            var (repoMod, structure) = CommonSetup();
            var storageMod = AddAction(structure, ModActionEnum.Use);

            var selection = CoreCalculation.AutoSelect(repoMod, structure);

            Assert.Equal(storageMod, selection?.StorageMod);
            Assert.Null(selection?.DownloadStorage);
        }

        [Fact]
        private void SingleUse_AllLoaded_Conflict()
        {
            var (repoMod, structure) = CommonSetup();
            var storageMod = AddAction(structure, ModActionEnum.Use);
            AddConflict(structure);

            var selection = CoreCalculation.AutoSelect(repoMod, structure);

            Assert.Null(selection?.StorageMod);
            Assert.Null(selection?.DownloadStorage);
        }

        [Fact]
        private void Precedence()
        {
            var (repoMod, structure) = CommonSetup();
            var storageMod = AddAction(structure, ModActionEnum.Use);
            AddAction(structure, ModActionEnum.Update);

            var selection = CoreCalculation.AutoSelect(repoMod, structure);

            Assert.Equal(storageMod, selection?.StorageMod);
            Assert.Null(selection?.DownloadStorage);
        }

        [Fact]
        private void Loading()
        {
            var (repoMod, structure) = CommonSetup();
            var storageMod = AddAction(structure, ModActionEnum.Use);
            AddAction(structure, ModActionEnum.Loading);

            var selection = CoreCalculation.AutoSelect(repoMod, structure);

            Assert.Null(selection?.StorageMod);
            Assert.Null(selection?.DownloadStorage);
        }
    }
}
