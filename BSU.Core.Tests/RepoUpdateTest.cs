using System.Collections.Generic;
using System.Threading;
using BSU.Core.Model.Updating;
using BSU.Core.Tests.Mocks;
using BSU.Core.Tests.Util;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests
{
    public class RepoUpdateTest : LoggedTest
    {
        public RepoUpdateTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        private void CheckCounts<T>(StageStats<T> args, int expectedSuccesses, int expectedFails)
        {
            var successes = args.Succeeded.Count;
            var fails = args.FailedCount;
            Assert.Equal(expectedSuccesses, successes);
            Assert.Equal(expectedFails, fails);
        }

        [Fact]
        private void Success()
        {
            var updateState = new MockUpdateState(false, false);
            var repoUpdate = new RepositoryUpdate(new List<IUpdateCreated>{updateState});

            var prepared = repoUpdate.Prepare(CancellationToken.None).Result;
            var done = prepared.Update(CancellationToken.None).Result;

            CheckCounts(prepared.Stats, 1, 0);
            CheckCounts(done.Stats, 1, 0);

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
        private void UpdateError()
        {
            var updateState = new MockUpdateState(false, true);
            var repoUpdate = new RepositoryUpdate(new List<IUpdateCreated> {updateState});

            var prepared = repoUpdate.Prepare(CancellationToken.None).Result;
            var done = prepared.Update(CancellationToken.None).Result;

            CheckCounts(prepared.Stats, 1, 0);
            CheckCounts(done.Stats, 0, 1);

            Assert.True(updateState.CommitCalled);
        }

        [Fact]
        private void PrepareError_KeepGoing()
        {
            var updateState = new MockUpdateState(false, false);
            var updateStateFail = new MockUpdateState(true, false);
            var repoUpdate = new RepositoryUpdate(new List<IUpdateCreated> {updateState, updateStateFail});

            var prepared = repoUpdate.Prepare(CancellationToken.None).Result;
            var done = prepared.Update(CancellationToken.None).Result;

            CheckCounts(prepared.Stats, 1, 1);
            CheckCounts(done.Stats, 1, 0);

            Assert.True(updateState.CommitCalled);
        }
    }
}
