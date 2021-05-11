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

        internal static (MockStorageMod, StorageMod, MockWorker) CreateStorageMod(UpdateTarget stateTarget = null)
        {
            var worker = new MockWorker(true);

            var mockStorage = new MockStorageMod();
            for (int i = 0; i < 3; i++)
            {
                mockStorage.SetFile($"/addons/asdf_{i}.pbo", "qwe");
            }

            var state = new MockPersistedStorageModState {UpdateTarget = stateTarget};

            var storageMod = new StorageMod(worker, mockStorage, "mystorage", state, worker, Guid.Empty, true);
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
        private void Created()
        {
            var (implementation, storageMod, worker) = CreateStorageMod();
            worker.DoWork();
            Assert.Equal(StorageModStateEnum.Created, storageMod.GetState());
        }

        [Fact]
        private void Matched()
        {
            var (implementation, storageMod, worker) = CreateStorageMod();
            worker.DoWork();
            storageMod.RequireMatchHash();
            worker.DoWork();
            Assert.Equal(StorageModStateEnum.Matched, storageMod.GetState());
            Assert.NotNull(storageMod.GetMatchHash());
            Assert.True(storageMod.GetMatchHash().IsMatch(new MatchHash(implementation)));
        }

        [Fact]
        private void Versioned()
        {
            var (implementation, storageMod, worker) = CreateStorageMod();
            worker.DoWork();
            storageMod.RequireMatchHash();
            worker.DoWork();
            storageMod.RequireVersionHash();
            worker.DoWork();
            Assert.Equal(StorageModStateEnum.Versioned, storageMod.GetState());
            Assert.NotNull(storageMod.GetMatchHash());
            Assert.True(storageMod.GetMatchHash().IsMatch(new MatchHash(implementation)));
            Assert.NotNull(storageMod.GetVersionHash());
            Assert.True(storageMod.GetVersionHash().IsMatch(new VersionHash(implementation)));
        }

        [Fact]
        private void CreatedWithTarget()
        {
            var target = new UpdateTarget("1234", "LeMod");
            var (implementation, storageMod, worker) = CreateStorageMod(target);
            worker.DoWork();
            Assert.Equal(StorageModStateEnum.CreatedWithUpdateTarget, storageMod.GetState());
            Assert.NotNull(storageMod.GetVersionHash());
            Assert.True(storageMod.GetVersionHash().IsMatch(VersionHash.FromDigest("1234")));
        }

        [Fact]
        private void Prepared()
        {
            var target = new UpdateTarget("123", "LeMod");
            var (implementation, storageMod, worker) = CreateStorageMod();

            var repo = CreateRepoMod();
            var update = storageMod.PrepareUpdate(repo, target, new MatchHash(new string[] { }),
                new VersionHash(new byte[] { }));
            update.Prepare().Wait();

            Assert.Equal(StorageModStateEnum.Updating, storageMod.GetState());
            Assert.NotNull(storageMod.GetMatchHash());
            Assert.True(storageMod.GetMatchHash().IsMatch(new MatchHash(new string[] { })));
            Assert.NotNull(storageMod.GetVersionHash());
            Assert.True(storageMod.GetVersionHash().IsMatch(new VersionHash(new byte[] { })));
        }

        [Fact]
        private void Updated()
        {
            var target = new UpdateTarget("123", "LeMod");
            var (implementation, storageMod, worker) = CreateStorageMod();

            var repo = CreateRepoMod();
            var update = storageMod.PrepareUpdate(repo, target, new MatchHash(new string[] { }),
                new VersionHash(new byte[] { }));

            update.Create().Wait();
            update.Prepare().Wait();
            update.Update().Wait();

            Assert.Equal(StorageModStateEnum.Created, storageMod.GetState());
            Assert.NotNull(storageMod.GetMatchHash());
            Assert.True(storageMod.GetMatchHash().IsMatch(new MatchHash(new string[] { })));
            Assert.NotNull(storageMod.GetVersionHash());
            Assert.True(storageMod.GetVersionHash().IsMatch(new VersionHash(new byte[] { })));
        }

        [Fact]
        private void UpdateAborted()
        {
            var (implementation, storageMod, worker) = CreateStorageMod();

            var repo = CreateRepoMod();
            var update = storageMod.PrepareUpdate(repo, new UpdateTarget("123", "LeMod"), new MatchHash(new string[] { }),
                new VersionHash(new byte[] { }));


            update.Create().Wait();
            update.Prepare().Wait();
            update.Abort();

            Assert.Equal(StorageModStateEnum.Created, storageMod.GetState());
            Assert.NotNull(storageMod.GetMatchHash());
            Assert.True(storageMod.GetMatchHash().IsMatch(new MatchHash(new string[] { })));
            Assert.NotNull(storageMod.GetVersionHash());
            Assert.True(storageMod.GetVersionHash().IsMatch(new VersionHash(new byte[] { })));
        }

        [Fact]
        private void ErrorLoad()
        {
            var (implementation, storageMod, worker) = CreateStorageMod();

            implementation.ThrowErrorLoad = true;

            storageMod.RequireMatchHash();
            worker.DoWork();
            Assert.Equal(StorageModStateEnum.Error, storageMod.GetState());
        }

        [Fact]
        private void ErrorUpdateDo()
        {
            // TODO: ???
            var (implementation, storageMod, worker) = CreateStorageMod();

            var repo = CreateRepoMod();
            var update = storageMod.PrepareUpdate(repo, new UpdateTarget("123", "LeMod"), new MatchHash(new string[] { }),
                new VersionHash(new byte[] { }));
            update.Create().Wait();
            update.Prepare().Wait();
            implementation.ThrowErrorOpen = true;
            update.Update().Wait();
            Assert.NotNull(update.Exception);
            Assert.Equal(StorageModStateEnum.Created, storageMod.GetState());
            implementation.ThrowErrorOpen = false;
            Assert.NotNull(storageMod.GetMatchHash());
            Assert.NotNull(storageMod.GetVersionHash());
        }

        [Fact]
        private void ErrorPrepareUpdate()
        {
            var (implementation, storageMod, worker) = CreateStorageMod();

            var repo = CreateRepoMod();
            var update = storageMod.PrepareUpdate(repo, new UpdateTarget("123", "LeMod"), new MatchHash(new string[] { }),
                new VersionHash(new byte[] { }));
            update.Create().Wait();
            implementation.ThrowErrorOpen = true;
            try
            {
                update.Prepare().Wait();
                Assert.False(true);
            }
            catch (TestException)
            {
            }

            Assert.Equal(StorageModStateEnum.Created, storageMod.GetState());
            Assert.NotNull(storageMod.GetMatchHash());
            Assert.NotNull(storageMod.GetVersionHash());
        }
    }
}
