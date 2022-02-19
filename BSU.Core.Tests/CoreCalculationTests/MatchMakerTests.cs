using BSU.Core.Model;
using BSU.Core.Services;
using BSU.Core.Tests.Util;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests.CoreCalculationTests
{
    public class MatchMakerTests : LoggedTest
    {
        public MatchMakerTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        private ModActionEnum DoCheck(int repoHash, int repoVersion, int? storageHash, int? storageVersion,  StorageModStateEnum state, bool canWrite = true)
        {
            // TODO: restructure a bit for less parameter passing all over the place
            var repoMod = new MockModelRepositoryMod(repoHash, repoVersion);
            var storageMod = new MockModelStorageMod(storageHash, storageVersion, state)
            {
                CanWrite = canWrite
            };

            return CoreCalculation.GetModAction(repoMod, storageMod);
        }


        [Fact]
        private void NoMatch()
        {
            var action = DoCheck(1, 1, 2, null, StorageModStateEnum.Created);

            Assert.Equal(ModActionEnum.Unusable, action);
        }

        [Fact]
        private void Use()
        {
            var action = DoCheck(1, 1, 1, 1, StorageModStateEnum.Created);

            Assert.Equal(ModActionEnum.Use, action);
        }

        [Fact]
        private void Update()
        {
            var action = DoCheck(1, 1, 1, 2, StorageModStateEnum.Created);

            Assert.Equal(ModActionEnum.Update, action);
        }

        [Fact]
        private void Error()
        {
            var action = DoCheck(1, 1, null, null, StorageModStateEnum.Error);

            Assert.Equal(ModActionEnum.Unusable, action);
        }

        [Fact]
        private void ContinueUpdate()
        {
            var action = DoCheck(1, 1, 1, 1, StorageModStateEnum.CreatedWithUpdateTarget);

            Assert.Equal(ModActionEnum.ContinueUpdate, action);
        }

        [Fact]
        private void AbortAndUpdate()
        {
            var action = DoCheck(2, 1, 1, 1, StorageModStateEnum.CreatedWithUpdateTarget);

            Assert.Equal(ModActionEnum.ContinueUpdate, action);
        }

        [Fact]
        private void Await()
        {
            var action = DoCheck(1, 1, 1, 1, StorageModStateEnum.Updating);

            Assert.Equal(ModActionEnum.Await, action);
        }

        [Fact]
        private void AbortActiveAndUpdate()
        {
            var action = DoCheck(1, 1, 1, 2, StorageModStateEnum.Updating);

            Assert.Equal(ModActionEnum.AbortActiveAndUpdate, action);
        }

        [Fact]
        private void DontUpdateSteam()
        {
            var action = DoCheck(1, 1, 1, 2, StorageModStateEnum.Created, false);

            Assert.Equal(ModActionEnum.Unusable, action);
        }
    }
}
