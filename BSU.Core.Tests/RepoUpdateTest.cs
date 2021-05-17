using System.Collections.Generic;
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
            var repoUpdate = new RepositoryUpdate(new List<IUpdateCreate>{updateState});

            var created = repoUpdate.Create().Result;
            var prepared = created.Prepare().Result;
            var done = prepared.Update().Result;

            CheckCounts(created.Stats, 1, 0);
            CheckCounts(prepared.Stats, 1, 0);
            CheckCounts(done.Stats, 1, 0);

            Assert.True(updateState.CommitCalled);
        }

        [Fact]
        private void PrepAbort()
        {
            var updateState = new MockUpdateState(false, false);
            var repoUpdate = new RepositoryUpdate(new List<IUpdateCreate> {updateState});

            var created = repoUpdate.Create().Result;
            var prepared = created.Prepare().Result;

            CheckCounts(created.Stats, 1, 0);
            CheckCounts(prepared.Stats, 1, 0);
            prepared.Abort();

            Assert.True(updateState.AbortCalled);
        }

        [Fact]
        private void SetupError()
        {
            var updateState = new MockUpdateState(true, false, false);
            var repoUpdate = new RepositoryUpdate(new List<IUpdateCreate> {updateState});

            var created = repoUpdate.Create().Result;
            CheckCounts(created.Stats, 0, 1);
            created.Abort();
            // TODO: check error propagation
        }

        [Fact]
        private void PrepError()
        {
            var updateState = new MockUpdateState(true, false);
            var repoUpdate = new RepositoryUpdate(new List<IUpdateCreate> {updateState});

            var created = repoUpdate.Create().Result;
            var prepared = created.Prepare().Result;

            CheckCounts(created.Stats, 1, 0);
            CheckCounts(prepared.Stats, 0, 1);
            prepared.Abort();

            Assert.False(updateState.CommitCalled);
        }

        [Fact]
        private void UpdateError()
        {
            var updateState = new MockUpdateState(false, true);
            var repoUpdate = new RepositoryUpdate(new List<IUpdateCreate> {updateState});

            var created = repoUpdate.Create().Result;
            var prepared = created.Prepare().Result;
            var done = prepared.Update().Result;

            CheckCounts(created.Stats, 1, 0);
            CheckCounts(prepared.Stats, 1, 0);
            CheckCounts(done.Stats, 0, 1);

            Assert.True(updateState.CommitCalled);
        }

        [Fact]
        private void SetupError_KeepGoing()
        {
            var updateState = new MockUpdateState(false, false);
            var updateStateFail = new MockUpdateState(true, false, false);
            var repoUpdate = new RepositoryUpdate(new List<IUpdateCreate> {updateState, updateStateFail});

            var created = repoUpdate.Create().Result;
            var prepared = created.Prepare().Result;
            var done = prepared.Update().Result;

            CheckCounts(created.Stats, 1, 1);
            CheckCounts(prepared.Stats, 1, 0);
            CheckCounts(done.Stats, 1, 0);

            Assert.True(updateState.CommitCalled);
        }

        [Fact]
        private void PrepareError_KeepGoing()
        {
            var updateState = new MockUpdateState(false, false);
            var updateStateFail = new MockUpdateState(true, false);
            var repoUpdate = new RepositoryUpdate(new List<IUpdateCreate> {updateState, updateStateFail});

            var created = repoUpdate.Create().Result;
            var prepared = created.Prepare().Result;
            var done = prepared.Update().Result;

            CheckCounts(created.Stats, 2, 0);
            CheckCounts(prepared.Stats, 1, 1);
            CheckCounts(done.Stats, 1, 0);

            Assert.True(updateState.CommitCalled);
        }
    }
}
