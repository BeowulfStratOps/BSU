using System;
using System.Collections.Generic;
using BSU.Core.Model;
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

        private void CheckCounts(StageCallbackArgs args, int expectedSuccesses, int expectedFails)
        {
            var successes = args.Succeeded.Count;
            var fails = args.Failed.Count;
            Assert.Equal(expectedSuccesses, successes);
            Assert.Equal(expectedFails, fails);
        }

        [Fact]
        private void Success()
        {
            var repoUpdate = new RepositoryUpdate();
            var updateState = new MockUpdateState(false, false);
            
            repoUpdate.Add(updateState);

            CheckCounts(repoUpdate.Create().Result, 1, 0);
            CheckCounts(repoUpdate.Prepare().Result, 1, 0);
            CheckCounts(repoUpdate.Update().Result, 1, 0);

            Assert.True(updateState.CommitCalled);
        }

        [Fact]
        private void PrepAbort()
        {
            var repoUpdate = new RepositoryUpdate();

            var updateState = new MockUpdateState(false, false);
            repoUpdate.Add(updateState);
            
            CheckCounts(repoUpdate.Create().Result, 1, 0);
            CheckCounts(repoUpdate.Prepare().Result, 1, 0);
            repoUpdate.Abort();

            Assert.True(updateState.AbortCalled);
        }

        [Fact]
        private void SetupError()
        {
            var repoUpdate = new RepositoryUpdate();

            repoUpdate.Add(new MockUpdateState(true, false, false));
            
            CheckCounts(repoUpdate.Create().Result, 0, 1);
            repoUpdate.Abort();
            // TODO: check error propagation
        }

        [Fact]
        private void PrepError()
        {
            var repoUpdate = new RepositoryUpdate();

            var updateState = new MockUpdateState(true, false);
            repoUpdate.Add(updateState);
            
            CheckCounts(repoUpdate.Create().Result, 1, 0);
            CheckCounts(repoUpdate.Prepare().Result, 0, 1);
            repoUpdate.Abort();

            Assert.False(updateState.CommitCalled);
        }

        [Fact]
        private void UpdateError()
        {
            var repoUpdate = new RepositoryUpdate();

            var updateState = new MockUpdateState(false, true);
            repoUpdate.Add(updateState);
            
            CheckCounts(repoUpdate.Create().Result, 1, 0);
            CheckCounts(repoUpdate.Prepare().Result, 1, 0);
            CheckCounts(repoUpdate.Update().Result, 0, 1);

            Assert.True(updateState.CommitCalled);
        }

        [Fact]
        private void SetupError_KeepGoing()
        {
            var repoUpdate = new RepositoryUpdate();

            var updateState = new MockUpdateState(false, false);
            repoUpdate.Add(updateState);
            repoUpdate.Add(new MockUpdateState(true, false, false));

            CheckCounts(repoUpdate.Create().Result, 1, 1);
            CheckCounts(repoUpdate.Prepare().Result, 1, 0);
            CheckCounts(repoUpdate.Update().Result, 1, 0);

            Assert.True(updateState.CommitCalled);
        }

        [Fact]
        private void PrepareError_KeepGoing()
        {
            var repoUpdate = new RepositoryUpdate();

            var updateState = new MockUpdateState(false, false);
            repoUpdate.Add(updateState);
            var updateStateFail = new MockUpdateState(true, false);
            repoUpdate.Add(updateStateFail);
            
            CheckCounts(repoUpdate.Create().Result, 2, 0);
            CheckCounts(repoUpdate.Prepare().Result, 1, 1);
            CheckCounts(repoUpdate.Update().Result, 1, 0);

            Assert.True(updateState.CommitCalled);
        }
    }
}
