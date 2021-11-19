using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.Core.Services;
using BSU.Core.Tests.Util;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests.CoreCalculationTests
{
    public class ConflictTests : LoggedTest
    {
        public ConflictTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        private async Task HasConflict()
        {
            var repoMod1 = new MockModelRepositoryMod(1, 1);

            var repoMod2 = new MockModelRepositoryMod(1, 2);

            var storageMod = new MockModelStorageMod(1, 1, StorageModStateEnum.Created);

            Assert.True(await CoreCalculation.IsConflicting(repoMod1, repoMod2, storageMod, CancellationToken.None));
        }

        [Fact]
        private async Task NoConflict()
        {
            var repoMod1 = new MockModelRepositoryMod(1, 1);

            var repoMod2 = new MockModelRepositoryMod(1, 1);

            var storageMod = new MockModelStorageMod(1, 1, StorageModStateEnum.Created);

            Assert.False(await CoreCalculation.IsConflicting(repoMod1, repoMod2, storageMod, CancellationToken.None));
        }
    }
}
