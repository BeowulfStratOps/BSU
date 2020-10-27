using System;
using System.Collections.Generic;
using BSU.Core.Model;
using BSU.Core.Tests.Mocks;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests
{
    public class RepoUpdateTest : LoggedTest, IDisposable
    {
        private readonly FuncCheck _funcCheck;

        public RepoUpdateTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _funcCheck = new FuncCheck();
        }

        public void Dispose()
        {
            _funcCheck.Check();
        }

        private class FuncCheck
        {
            private readonly List<string> _calls = new List<string>();

            public RepositoryUpdate.SetUpDelegate GetSetUp(int succeeded, int errored, bool proceed)
            {
                _calls.Remove(nameof(RepositoryUpdate.SetUpDelegate));
                void Func(StageCallbackArgs args, Action<bool> _proceed)
                {
                    Assert.Equal(succeeded, args.Succeeded.Count);
                    Assert.Equal(errored, args.Failed.Count);
                    _calls.Remove(nameof(RepositoryUpdate.SetUpDelegate));
                    _proceed(proceed);
                }
                return Func;
            }

            public RepositoryUpdate.SetUpDelegate GetSetUp()
            {
                void Func(StageCallbackArgs args, Action<bool> _proceed)
                {
                    Assert.False(true);
                }
                return Func;
            }

            public RepositoryUpdate.PreparedDelegate GetPrepared(int succeeded, int errored, bool proceed)
            {
                _calls.Add(nameof(RepositoryUpdate.PreparedDelegate));
                void Func(StageCallbackArgs args, Action<bool> _proceed)
                {
                    Assert.Equal(succeeded, args.Succeeded.Count);
                    Assert.Equal(errored, args.Failed.Count);
                    _calls.Remove(nameof(RepositoryUpdate.PreparedDelegate));
                    _proceed(proceed);
                }
                return Func;
            }

            public RepositoryUpdate.PreparedDelegate GetPrepared()
            {
                void Func(StageCallbackArgs args, Action<bool> _proceed)
                {
                    Assert.False(true);
                }
                return Func;
            }

            public RepositoryUpdate.FinishedDelegate GetFinished(int succeeded, int errored)
            {
                _calls.Add(nameof(RepositoryUpdate.FinishedDelegate));
                void Func(StageCallbackArgs args)
                {
                    Assert.Equal(succeeded, args.Succeeded.Count);
                    Assert.Equal(errored, args.Failed.Count);
                    _calls.Remove(nameof(RepositoryUpdate.FinishedDelegate));
                }
                return Func;
            }

            public RepositoryUpdate.FinishedDelegate GetFinished()
            {
                void Func(StageCallbackArgs args)
                {
                    Assert.False(true);
                }
                return Func;
            }

            public void Check()
            {
                Assert.Empty(_calls);
            }
        }

        [Fact]
        private void Success()
        {
            var repoUpdate = new RepositoryUpdate(
                _funcCheck.GetSetUp(),
                _funcCheck.GetPrepared(1, 0, true),
                _funcCheck.GetFinished(1, 0));
            var updateState = new MockUpdateState(false, false);

            repoUpdate.Add(updateState);
            repoUpdate.Start();

            Assert.True(updateState.CommitCalled);
        }

        [Fact]
        private void PrepAbort()
        {
            var repoUpdate = new RepositoryUpdate(
                _funcCheck.GetSetUp(),
                _funcCheck.GetPrepared(1, 0, false),
                _funcCheck.GetFinished());

            var updateState = new MockUpdateState(false, false);
            repoUpdate.Add(updateState);
            repoUpdate.Start();

            Assert.True(updateState.AbortCalled);
        }

        [Fact]
        private void SetupError()
        {
            var repoUpdate = new RepositoryUpdate(
                _funcCheck.GetSetUp(0, 1, false),
                _funcCheck.GetPrepared(),
                _funcCheck.GetFinished());

            repoUpdate.Add(new MockUpdateState(true, false, false));
            repoUpdate.Start();
        }

        [Fact]
        private void PrepError()
        {
            var repoUpdate = new RepositoryUpdate(
                _funcCheck.GetSetUp(),
                _funcCheck.GetPrepared(0, 1, false),
                _funcCheck.GetFinished());

            var updateState = new MockUpdateState(true, false);
            repoUpdate.Add(updateState);
            repoUpdate.Start();

            Assert.False(updateState.CommitCalled);
        }

        [Fact]
        private void UpdateError()
        {
            var repoUpdate = new RepositoryUpdate(
                _funcCheck.GetSetUp(),
                _funcCheck.GetPrepared(1, 0, true),
                _funcCheck.GetFinished(0, 1));

            var updateState = new MockUpdateState(false, true);
            repoUpdate.Add(updateState);
            repoUpdate.Start();

            Assert.True(updateState.CommitCalled);
        }

        [Fact]
        private void SetupError_KeepGoing()
        {
            var repoUpdate = new RepositoryUpdate(
                _funcCheck.GetSetUp(1, 1, true),
                _funcCheck.GetPrepared(1, 0, true),
                _funcCheck.GetFinished(1, 0));

            var updateState = new MockUpdateState(false, false);
            repoUpdate.Add(updateState);
            repoUpdate.Add(new MockUpdateState(true, false, false));

            repoUpdate.Start();

            Assert.True(updateState.CommitCalled);
        }

        [Fact]
        private void PrepareError_KeepGoing()
        {
            var repoUpdate = new RepositoryUpdate(
                _funcCheck.GetSetUp(),
                _funcCheck.GetPrepared(1, 1, true),
                _funcCheck.GetFinished(1, 0));

            var updateState = new MockUpdateState(false, false);
            repoUpdate.Add(updateState);
            var updateStateFail = new MockUpdateState(true, false);
            repoUpdate.Add(updateStateFail);
            repoUpdate.Start();

            Assert.True(updateState.CommitCalled);
        }
    }
}
