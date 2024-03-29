﻿using System;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.Core.Model.Updating;
using BSU.Core.Sync;
using BSU.Core.Tests.Util;
using BSU.Core.ViewModel;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests
{
    public class RepoUpdateTest : LoggedTest
    {
        public RepoUpdateTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        private void CheckCounts(StageStats args, int expectedSuccesses, int expectedFails)
        {
            var successes = args.SucceededCount;
            var fails = args.Failed.Count + args.FailedSharingViolation.Count;
            Assert.Equal(expectedSuccesses, successes);
            Assert.Equal(expectedFails, fails);
        }

        private static async Task<StageStats> Update(params ModUpdate[] updates)
        {
            return await RepositoryUpdate.Update(updates.ToList(), null);
        }

        private static ModUpdate CreateModUpdate(Task<UpdateResult> update)
        {
            var mod = new Mock<IModelStorageMod>(MockBehavior.Strict).Object;
            return new ModUpdate(update, new Progress<FileSyncStats>(), mod);
        }

        [Fact]
        private async Task Success()
        {
            var updateState = CreateModUpdate(Task.FromResult(UpdateResult.Success));

            var done = await Update(updateState);

            CheckCounts(done, 1, 0);
        }

        [Fact]
        private async Task UpdateError()
        {
            var updateState = CreateModUpdate(Task.FromResult(UpdateResult.Failed));

            var done = await Update(updateState);

            CheckCounts(done, 0, 1);
        }
    }
}
