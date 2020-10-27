using System;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.Core.Tests.Mocks;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests
{
    public class StorageModLifecycleTests : LoggedTest
    {
        public StorageModLifecycleTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        internal static (MockStorageMod, StorageMod, MockWorker) CreateStorageMod(UpdateTarget stateTarget = null, UpdateTarget updateTarget = null)
        {
            var worker = new MockWorker();

            var mockStorage = new MockStorageMod();
            for (int i = 0; i < 3; i++)
            {
                mockStorage.SetFile($"/addons/asdf_{i}.pbo", "qwe");
            }

            var state = new MockStorageModState {UpdateTarget = stateTarget};
            IUpdateState downloadState = null;

            if (updateTarget != null)
                downloadState = new MockUpdateState(false, false, false);
            
            var storageMod = new StorageMod(worker, mockStorage, "mystorage", updateTarget, state, worker, Guid.Empty, true, downloadState);
            return (mockStorage, storageMod, worker);
        }

        internal static MockRepositoryMod CreateRepoMod()
        {
            var mockRepo = new MockRepositoryMod();
            for (int i = 0; i < 3; i++)
            {
                mockRepo.SetFile($"/addons/asdf_{i}.pbo", "asd");
            }

            return mockRepo;
        }

        [Fact]
        private void Loading()
        {
            var (implementation, storageMod, worker) = CreateStorageMod();

            Assert.Equal(StorageModStateEnum.Loading, storageMod.GetState().State);
            var state = storageMod.GetState();
            Assert.Null(state.MatchHash);
            Assert.Null(state.VersionHash);
            Assert.Null(state.Error);
            Assert.Null(state.UpdateTarget);
            Assert.Null(state.JobTarget);
        }

        [Fact]
        private void Loaded()
        {
            var (implementation, storageMod, worker) = CreateStorageMod();
            worker.DoWork();
            var state = storageMod.GetState();
            Assert.Equal(StorageModStateEnum.Loaded, state.State);
            Assert.NotNull(state.MatchHash);
            Assert.Null(state.VersionHash);
            Assert.Null(state.Error);
            Assert.Null(state.UpdateTarget);
            Assert.Null(state.JobTarget);
            Assert.True(state.MatchHash.IsMatch(new MatchHash(implementation)));
        }

        [Fact]
        private void Hashing()
        {
            var (implementation, storageMod, worker) = CreateStorageMod();
            worker.DoWork();
            storageMod.RequireHash();
            while (worker.DoQueueStep())
            {
            }

            var state = storageMod.GetState();
            Assert.Equal(StorageModStateEnum.Hashing, state.State);
            Assert.NotNull(state.MatchHash);
            Assert.Null(state.VersionHash);
            Assert.Null(state.Error);
            Assert.Null(state.UpdateTarget);
            Assert.Null(state.JobTarget);
        }

        [Fact]
        private void Hashed()
        {
            var (implementation, storageMod, worker) = CreateStorageMod();
            worker.DoWork();
            storageMod.RequireHash();
            worker.DoWork();

            var state = storageMod.GetState();
            Assert.Equal(StorageModStateEnum.Hashed, state.State);
            Assert.NotNull(state.MatchHash);
            Assert.NotNull(state.VersionHash);
            Assert.Null(state.Error);
            Assert.Null(state.UpdateTarget);
            Assert.Null(state.JobTarget);
            Assert.True(state.VersionHash.IsMatch(new VersionHash(implementation)));
        }

        [Fact]
        private void CreatedForDownload()
        {
            var target = new UpdateTarget("123", "LeMod");
            var (implementation, storageMod, worker) = CreateStorageMod(updateTarget: target);

            Assert.Equal(StorageModStateEnum.CreatedForDownload, storageMod.GetState().State);
            var state = storageMod.GetState();
            Assert.Null(state.MatchHash);
            Assert.NotNull(state.VersionHash);
            Assert.Null(state.Error);
            Assert.NotNull(state.UpdateTarget);
            Assert.Equal(state.UpdateTarget.Hash, target.Hash);
            Assert.Null(state.JobTarget);
            Assert.True(state.VersionHash.IsMatch(VersionHash.CreateEmpty()));
        }

        [Fact]
        private void Downloading()
        {
            var target = new UpdateTarget("123", "LeMod");
            var (implementation, storageMod, worker) = CreateStorageMod(updateTarget: target);

            var repo = CreateRepoMod();
            storageMod.PrepareUpdate(repo, target);
            worker.DoQueueStep();

            Assert.Equal(StorageModStateEnum.Updating, storageMod.GetState().State);
            var state = storageMod.GetState();
            Assert.Null(state.MatchHash);
            Assert.Null(state.VersionHash);
            Assert.Null(state.Error);
            Assert.NotNull(state.UpdateTarget);
            Assert.Equal(state.UpdateTarget.Hash, target.Hash);
            Assert.NotNull(state.JobTarget);
            Assert.Equal(state.JobTarget.Hash, target.Hash);
        }

        [Fact]
        private void Downloaded()
        {
            var target = new UpdateTarget("123", "LeMod");
            var (implementation, storageMod, worker) = CreateStorageMod(updateTarget: target);

            var repo = CreateRepoMod();
            var update = storageMod.PrepareUpdate(repo, target);
            update.OnStateChange += () =>
            {
                if (update.State == UpdateState.Prepared)
                    update.Continue();
            };
            update.Continue();
            worker.DoWork();

            Assert.Equal(StorageModStateEnum.Loaded, storageMod.GetState().State);
            var state = storageMod.GetState();
            Assert.NotNull(state.MatchHash);
            Assert.Null(state.VersionHash);
            Assert.Null(state.Error);
            Assert.Null(state.UpdateTarget);
            Assert.Null(state.JobTarget);
            Assert.True(state.MatchHash.IsMatch(new MatchHash(repo)));
        }

        [Fact]
        private void WithTarget()
        {
            var (implementation, storageMod, worker) = CreateStorageMod();

            Assert.Equal(StorageModStateEnum.Loading, storageMod.GetState().State);
            var state = storageMod.GetState();
            Assert.Null(state.MatchHash);
            Assert.Null(state.VersionHash);
            Assert.Null(state.Error);
            Assert.Null(state.UpdateTarget);
            Assert.Null(state.JobTarget);
        }

        [Fact]
        private void Updating()
        {
            var target = new UpdateTarget("123", "LeMod");
            var (implementation, storageMod, worker) = CreateStorageMod();

            worker.DoWork();
            storageMod.RequireHash();
            worker.DoWork();

            var repo = CreateRepoMod();
            storageMod.PrepareUpdate(repo, target);
            worker.DoQueueStep();

            Assert.Equal(StorageModStateEnum.Updating, storageMod.GetState().State);
            var state = storageMod.GetState();
            Assert.Null(state.MatchHash);
            Assert.Null(state.VersionHash);
            Assert.Null(state.Error);
            Assert.NotNull(state.UpdateTarget);
            Assert.Equal(state.UpdateTarget.Hash, target.Hash);
            Assert.NotNull(state.JobTarget);
            Assert.Equal(state.JobTarget.Hash, target.Hash);
        }

        [Fact]
        private void Updated()
        {
            var target = new UpdateTarget("123", "LeMod");
            var (implementation, storageMod, worker) = CreateStorageMod();

            worker.DoWork();
            storageMod.RequireHash();
            worker.DoWork();

            var repo = CreateRepoMod();
            var update = storageMod.PrepareUpdate(repo, target);
            update.OnStateChange += () =>
            {
                if (update.State == UpdateState.Prepared)
                    update.Continue();
            };

            update.Continue();
            worker.DoWork();

            Assert.Equal(StorageModStateEnum.Loaded, storageMod.GetState().State);
            var state = storageMod.GetState();
            Assert.NotNull(state.MatchHash);
            Assert.Null(state.VersionHash);
            Assert.Null(state.Error);
            Assert.Null(state.UpdateTarget);
            Assert.Null(state.JobTarget);
            Assert.True(state.MatchHash.IsMatch(new MatchHash(repo)));
        }

        [Fact]
        private void UpdateAborted()
        {
            var (implementation, storageMod, worker) = CreateStorageMod();

            worker.DoWork();
            storageMod.RequireHash();
            worker.DoWork();

            var repo = CreateRepoMod();
            var update = storageMod.PrepareUpdate(repo, new UpdateTarget("123", "LeMod"));
            update.OnStateChange += () =>
            {
                if (update.State == UpdateState.Prepared)
                    update.Abort();
            };

            update.Continue();
            worker.DoWork();

            Assert.Equal(StorageModStateEnum.Loaded, storageMod.GetState().State);
            var state = storageMod.GetState();
            Assert.NotNull(state.MatchHash);
            Assert.Null(state.VersionHash);
            Assert.Null(state.Error);
            Assert.Null(state.UpdateTarget);
            Assert.Null(state.JobTarget);
        }

        [Fact]
        private void ErrorLoad()
        {
            var (implementation, storageMod, worker) = CreateStorageMod();

            implementation.ThrowErrorLoad = true;
            worker.DoWork();

            var state = storageMod.GetState();
            Assert.Equal(StorageModStateEnum.ErrorLoad, state.State);
        }

        [Fact]
        private void ErrorHash()
        {
            var (implementation, storageMod, worker) = CreateStorageMod();

            worker.DoWork();
            storageMod.RequireHash();
            implementation.ThrowErrorOpen = true;
            worker.DoWork();

            var state = storageMod.GetState();
            Assert.Equal(StorageModStateEnum.ErrorLoad, state.State);
        }

        [Fact]
        private void ErrorUpdateDo()
        {
            var (implementation, storageMod, worker) = CreateStorageMod();

            worker.DoWork();
            storageMod.RequireHash();
            worker.DoWork();

            var repo = CreateRepoMod();
            var update = storageMod.PrepareUpdate(repo, new UpdateTarget("123", "LeMod"));
            update.OnStateChange += () =>
            {
                if (update.State != UpdateState.Prepared) return;
                implementation.ThrowErrorOpen = true;
                update.Continue();
            };
            update.Continue();

            worker.DoWork();

            var state = storageMod.GetState();
            Assert.Equal(StorageModStateEnum.ErrorUpdate, state.State);
        }

        [Fact]
        private void ErrorPrepareUpdate()
        {
            var target = new UpdateTarget("123", "LeMod");
            var (implementation, storageMod, worker) = CreateStorageMod();

            worker.DoWork();
            storageMod.RequireHash();
            worker.DoWork();

            var repo = CreateRepoMod();
            Exception error = null;
            implementation.ThrowErrorOpen = true;
            var update = storageMod.PrepareUpdate(repo, target);
            update.OnStateChange += () =>
            {
                if (update.State != UpdateState.Errored) return;
                error = update.Exception;
            };
            update.Continue();
            worker.DoWork();

            Assert.NotNull(error);
            Assert.IsType<TestException>(error);
        }
    }
}
