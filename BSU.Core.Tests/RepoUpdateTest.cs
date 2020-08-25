using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.Model;
using BSU.Core.Model.Utility;
using BSU.Core.Tests.Mocks;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests
{
    public class RepoUpdateTest : LoggedTest
    {
        public RepoUpdateTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        // TODO: use disposable callbacks with using, check that they were called before being disposed.

        [Fact]
        private void Success()
        {
            var repoUpdate = new RepositoryUpdate();
            repoUpdate.OnSetup += (_, __, proceed) => proceed(true);
            repoUpdate.OnPrepared += (_, __, proceed) => proceed(true);
            var repoSuccess = false;
            repoUpdate.OnFinished += (_, errored) => { if (!errored.Any()) repoSuccess = true; };
            var updateState = new MockUpdateState(false, false);
            repoUpdate.Add(updateState);
            repoUpdate.DoneAdding();

            Assert.True(updateState.CommitCalled);
            Assert.True(repoSuccess);
        }

        [Fact]
        private void PrepAbort()
        {
            var repoUpdate = new RepositoryUpdate();
            repoUpdate.OnSetup += (_, __, proceed) => proceed(true);
            repoUpdate.OnPrepared += (_, __, proceed) => proceed(false);
            var success = true;
            repoUpdate.OnFinished += (_, errored) => { success = false; };
            var updateState = new MockUpdateState(false, false);
            repoUpdate.Add(updateState);
            repoUpdate.DoneAdding();


            Assert.True(updateState.AbortCalled);
            Assert.True(success);
        }

        [Fact]
        private void SetupError()
        {
            var repoUpdate = new RepositoryUpdate();
            var setupErrored = false;
            repoUpdate.OnSetup += (_, errors, proceed) =>
            {
                if (errors.Any()) setupErrored = true;
                proceed(true);
            };
            var p = new Promise<IUpdateState>();
            var info = new DownloadInfo(null, "", p);
            repoUpdate.Add(info);
            repoUpdate.DoneAdding();

            p.Error(new TestException());

            Assert.True(setupErrored);
        }

        [Fact]
        private void PrepError()
        {
            var repoUpdate = new RepositoryUpdate();
            repoUpdate.OnSetup += (_, __, proceed) => proceed(true);
            var prepareErrored = false;
            repoUpdate.OnPrepared += (_, errored, proceed) =>
            {
                if (errored.Any()) prepareErrored = true;
            };
            var updateState = new MockUpdateState(true, false);
            repoUpdate.Add(updateState);
            repoUpdate.DoneAdding();

            Assert.False(updateState.CommitCalled);
            Assert.True(prepareErrored);
        }

        [Fact]
        private void UpdateError()
        {
            var repoUpdate = new RepositoryUpdate();
            repoUpdate.OnSetup += (_, __, proceed) => proceed(true);
            repoUpdate.OnPrepared += (_, __, proceed) => proceed(true);
            var updateErrored = false;
            repoUpdate.OnFinished += (_, errored) => { if (errored.Any()) updateErrored = true; };
            var updateState = new MockUpdateState(false, true);
            repoUpdate.Add(updateState);
            repoUpdate.DoneAdding();

            Assert.True(updateState.CommitCalled);
            Assert.True(updateErrored);
        }

        [Fact]
        private void SetupError_KeepGoing()
        {
            var repoUpdate = new RepositoryUpdate();
            repoUpdate.OnSetup += (_, errored, proceed) =>
            {
                Assert.NotEmpty(errored);
                proceed(true);
            };
            repoUpdate.OnPrepared += (_, __, proceed) => proceed(true);
            var repoSuccess = false;
            repoUpdate.OnFinished += (_, errored) => { if (!errored.Any()) repoSuccess = true; };

            var updateState = new MockUpdateState(false, false);
            repoUpdate.Add(updateState);

            var pFail = new Promise<IUpdateState>();
            var info = new DownloadInfo(null, "", pFail);
            repoUpdate.Add(info);

            repoUpdate.DoneAdding();

            pFail.Error(new TestException());

            Assert.True(updateState.CommitCalled);
            Assert.True(repoSuccess);
        }

        [Fact]
        private void PrepareError_KeepGoing()
        {
            var repoUpdate = new RepositoryUpdate();
            repoUpdate.OnSetup += (_, errored, proceed) =>
            {
                Assert.Empty(errored);
                proceed(true);
            };
            repoUpdate.OnPrepared += (_, errored, proceed) =>
            {
                Assert.Single(errored);
                proceed(true);
            };
            var repoSuccess = false;
            repoUpdate.OnFinished += (_, errored) => { if (!errored.Any()) repoSuccess = true; };

            var updateState = new MockUpdateState(false, false);
            repoUpdate.Add(updateState);
            var updateStateFail = new MockUpdateState(true, false);
            repoUpdate.Add(updateStateFail);
            repoUpdate.DoneAdding();

            Assert.True(updateState.CommitCalled);
            Assert.True(updateStateFail.AbortCalled);
            Assert.True(repoSuccess);
        }
    }
}
