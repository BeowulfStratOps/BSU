using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model.Updating;
using BSU.Core.Sync;
using BSU.Core.Tests.Mocks;
using BSU.Core.Tests.Util;
using BSU.Core.ViewModel;
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

        private RepositoryUpdate GetRepoUpdate(params IModUpdate[] updates)
        {
            return new RepositoryUpdate(updates.Select(u => (u, new Progress<FileSyncStats>())).ToList(), null);
        }

        [Fact]
        private async Task Success()
        {
            var updateState = new MockUpdateState(false, false, null);
            var repoUpdate = GetRepoUpdate(updateState);

            var prepared = await repoUpdate.Prepare(CancellationToken.None);
            var done = await repoUpdate.Update(CancellationToken.None);

            CheckCounts(prepared, 1, 0);
            CheckCounts(done, 1, 0);

            Assert.True(updateState.CommitCalled);
        }

        /*[Fact]
        private void PrepAbort()
        {
            var updateState = new MockUpdateState(false, false);
            var repoUpdate = new RepositoryUpdate(new List<IUpdateCreated> {updateState});

            var prepared = repoUpdate.Prepare(CancellationToken.None).Result;

            CheckCounts(prepared.Stats, 1, 0);
            prepared.Abort();

            Assert.True(updateState.AbortCalled);
        }

        [Fact]
        private void PrepError()
        {
            var updateState = new MockUpdateState(true, false);
            var repoUpdate = new RepositoryUpdate(new List<IUpdateCreated> {updateState});

            var prepared = repoUpdate.Prepare(CancellationToken.None).Result;

            CheckCounts(prepared.Stats, 0, 1);
            prepared.Abort();

            Assert.False(updateState.CommitCalled);
        }*/

        [Fact]
        private async Task UpdateError()
        {
            var updateState = new MockUpdateState(false, true, null);
            var repoUpdate = GetRepoUpdate(updateState);

            var prepared = await repoUpdate.Prepare(CancellationToken.None);
            var done = await repoUpdate.Update(CancellationToken.None);

            CheckCounts(prepared, 1, 0);
            CheckCounts(done, 0, 1);

            Assert.True(updateState.CommitCalled);
        }

        [Fact]
        private async Task PrepareError_KeepGoing()
        {
            var updateState = new MockUpdateState(false, false, null);
            var updateStateFail = new MockUpdateState(true, false, null);
            var repoUpdate = GetRepoUpdate(updateState, updateStateFail);

            var prepared = await repoUpdate.Prepare(CancellationToken.None);
            var done = await repoUpdate.Update(CancellationToken.None);

            CheckCounts(prepared, 1, 1);
            CheckCounts(done, 1, 0);

            Assert.True(updateState.CommitCalled);
        }
    }
}
