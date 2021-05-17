using System;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.Core.Tests.Mocks;
using BSU.Core.Tests.Util;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests
{
    public class StorageModLifecycleTests : LoggedTest
    {
        public StorageModLifecycleTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        internal static (MockStorageMod, StorageMod, MockWorker) CreateStorageMod(UpdateTarget stateTarget = null, bool load = true, bool loadVersion = false)
        {
            var worker = new MockWorker(true);

            var mockStorage = new MockStorageMod();
            for (int i = 0; i < 3; i++)
            {
                mockStorage.SetFile($"/addons/asdf_{i}.pbo", "qwe");
            }

            var state = new MockPersistedStorageModState {UpdateTarget = stateTarget};

            var storageMod = new StorageMod(worker, mockStorage, "mystorage", state, worker, Guid.Empty, true);
            if (load)
            {
                storageMod.Load();
                worker.DoWork();
            }

            if (loadVersion)
            {
                storageMod.RequireMatchHash();
                worker.DoWork();
                storageMod.RequireVersionHash();
                worker.DoWork();
            }

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
            Assert.Equal(StorageModStateEnum.Loaded, storageMod.GetState());
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
            Assert.Equal(StorageModStateEnum.LoadedWithUpdateTarget, storageMod.GetState());
            Assert.NotNull(storageMod.GetVersionHash());
            Assert.True(storageMod.GetVersionHash().IsMatch(VersionHash.FromDigest("1234")));
        }

        [Fact]
        private void MatchedWithTarget()
        {
            var target = new UpdateTarget("1234", "LeMod");
            var (implementation, storageMod, worker) = CreateStorageMod(target);
            storageMod.RequireMatchHash();
            worker.DoWork();
            Assert.Equal(StorageModStateEnum.MatchedWithUpdateTarget, storageMod.GetState());
            Assert.NotNull(storageMod.GetMatchHash());
            Assert.True(storageMod.GetMatchHash().IsMatch(new MatchHash(implementation)));
        }

        [Fact]
        private void UpdateWithTarget()
        {
            var target = new UpdateTarget("1234", "LeMod");
            var (implementation, storageMod, worker) = CreateStorageMod(target);

            storageMod.RequireMatchHash();
            worker.DoWork();

            var repo = CreateRepoMod();
            var update = storageMod.PrepareUpdate(repo, target, new MatchHash(new string[] { }),
                new VersionHash(new byte[] { }));

            Assert.Equal(StorageModStateEnum.Updating, storageMod.GetState());
            Assert.NotNull(storageMod.GetMatchHash());
            Assert.True(storageMod.GetMatchHash().IsMatch(new MatchHash(new string[] { })));
            Assert.NotNull(storageMod.GetVersionHash());
            Assert.True(storageMod.GetVersionHash().IsMatch(new VersionHash(new byte[] { })));
        }

        [Fact]
        private void Prepared()
        {
            var target = new UpdateTarget("123", "LeMod");
            var (implementation, storageMod, worker) = CreateStorageMod(loadVersion: true);

            var repo = CreateRepoMod();
            var update = storageMod.PrepareUpdate(repo, target, new MatchHash(new string[] { }),
                new VersionHash(new byte[] { }));

            Assert.Equal(StorageModStateEnum.Updating, storageMod.GetState());

            update.Create().Result.Prepare().Wait();

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
            var (implementation, storageMod, worker) = CreateStorageMod(loadVersion: true);

            var repo = CreateRepoMod();
            var update = storageMod.PrepareUpdate(repo, target, new MatchHash(new string[] { }),
                new VersionHash(new byte[] { }));

            var created = update.Create().Result;
            var prepared = created.Prepare().Result;
            prepared.Update().Wait();

            Assert.Equal(StorageModStateEnum.Loaded, storageMod.GetState());
            Assert.Throws<InvalidOperationException>(() => storageMod.GetMatchHash());
            Assert.Throws<InvalidOperationException>(() => storageMod.GetVersionHash());
        }

        [Fact]
        private void UpdateAborted()
        {
            var (implementation, storageMod, worker) = CreateStorageMod(loadVersion: true);

            var repo = CreateRepoMod();
            var update = storageMod.PrepareUpdate(repo, new UpdateTarget("123", "LeMod"), new MatchHash(new string[] { }),
                new VersionHash(new byte[] { }));


            var created = update.Create().Result;
            var prepared = created.Prepare().Result;
            prepared.Abort();

            Assert.Equal(StorageModStateEnum.Loaded, storageMod.GetState());
            Assert.Throws<InvalidOperationException>(() => storageMod.GetMatchHash());
            Assert.Throws<InvalidOperationException>(() => storageMod.GetVersionHash());
        }

        [Fact]
        private void ErrorLoad()
        {
            var (implementation, storageMod, worker) = CreateStorageMod(load: false);

            implementation.ThrowErrorLoad = true;

            storageMod.Load();
            worker.DoWork();
            Assert.Equal(StorageModStateEnum.Error, storageMod.GetState());
        }

        [Fact]
        private void ErrorUpdateDo()
        {
            // TODO: ???
            var (implementation, storageMod, worker) = CreateStorageMod(loadVersion: true);

            var repo = CreateRepoMod();
            var update = storageMod.PrepareUpdate(repo, new UpdateTarget("123", "LeMod"), new MatchHash(new string[] { }),
                new VersionHash(new byte[] { }));
            var created = update.Create().Result;
            var prepared = created.Prepare().Result;
            implementation.ThrowErrorOpen = true;
            Assert.ThrowsAsync<TestException>(() => prepared.Update());
            Assert.Equal(StorageModStateEnum.Loaded, storageMod.GetState());
            implementation.ThrowErrorOpen = false;
            Assert.Throws<InvalidOperationException>(() => storageMod.GetMatchHash());
            Assert.Throws<InvalidOperationException>(() => storageMod.GetVersionHash());
        }

        [Fact]
        private void ErrorPrepareUpdate()
        {
            var (implementation, storageMod, worker) = CreateStorageMod(loadVersion: true);

            var repo = CreateRepoMod();
            var update = storageMod.PrepareUpdate(repo, new UpdateTarget("123", "LeMod"), new MatchHash(new string[] { }),
                new VersionHash(new byte[] { }));
            var created = update.Create().Result;
            implementation.ThrowErrorOpen = true;
            Assert.ThrowsAsync<TestException>(() => created.Prepare());
            Assert.Equal(StorageModStateEnum.Loaded, storageMod.GetState());
            Assert.Throws<InvalidOperationException>(() => storageMod.GetMatchHash());
            Assert.Throws<InvalidOperationException>(() => storageMod.GetVersionHash());
        }
    }
}
