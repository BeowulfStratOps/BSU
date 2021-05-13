using System.Collections.Generic;
using System.Linq;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.Core.Tests.CoreCalculationTests;
using BSU.Core.Tests.Mocks;
using BSU.Core.Tests.Util;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests
{
    public class ConflictTests : LoggedTest
    {
        public ConflictTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        private void HasConflict()
        {
            var repoMod1 = new MockModelRepositoryMod(1, 1);

            var repoMod2 = new MockModelRepositoryMod(1, 2);

            var storageMod = new MockModelStorageMod(1, 1, StorageModStateEnum.Versioned);

            var structure = new MockModelStructure {RepositoryMods = {repoMod1, repoMod2}};

            var conflicts = CoreCalculation.GetConflicts(repoMod1, storageMod, structure);

            Assert.Single(conflicts);
        }

        [Fact]
        private void NoConflict()
        {
            var repoMod1 = new MockModelRepositoryMod(1, 1);

            var repoMod2 = new MockModelRepositoryMod(1, 1);

            var storageMod = new MockModelStorageMod(1, 1, StorageModStateEnum.Versioned);

            var structure = new MockModelStructure {RepositoryMods = {repoMod1, repoMod2}};

            var conflicts = CoreCalculation.GetConflicts(repoMod1, storageMod, structure);

            Assert.Empty(conflicts);
        }
    }
}
