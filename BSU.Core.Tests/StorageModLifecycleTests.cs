using System;
using System.Threading;
using System.Threading.Tasks;
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

        internal static (MockStorageMod, StorageMod, TestEventBus) CreateStorageMod(UpdateTarget stateTarget = null)
        {
            var mockStorage = new MockStorageMod();
            for (int i = 0; i < 3; i++)
            {
                mockStorage.SetFile($"/addons/asdf_{i}.pbo", "qwe");
            }

            var state = new MockPersistedStorageModState {UpdateTarget = stateTarget};

            var eventBus = new TestEventBus();
            var storageMod = new StorageMod(mockStorage, "mystorage", state, null, true, eventBus);


            return (mockStorage, storageMod, eventBus);
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
            var (implementation, storageMod, eventBus) = CreateStorageMod();
            eventBus.WorkUntil(100, () => storageMod.GetState() != StorageModStateEnum.Loading);
            Assert.Equal(StorageModStateEnum.Created, storageMod.GetState());
        }

        [Fact]
        private void CreatedWithTarget()
        {
            var target = new UpdateTarget("1234");
            var (implementation, storageMod, _) = CreateStorageMod(target);
            Assert.Equal(StorageModStateEnum.CreatedWithUpdateTarget, storageMod.GetState());
            Assert.NotNull(storageMod.GetVersionHash());
            var versionHash = storageMod.GetVersionHash();
            Assert.True(versionHash.IsMatch(VersionHash.FromDigest("1234")));
        }

        [Fact]
        private void UpdateWithTarget()
        {
            var target = new UpdateTarget("1234");
            var (implementation, storageMod, _) = CreateStorageMod(target);


            var repo = CreateRepoMod();
            var update = storageMod.PrepareUpdate(repo, MatchHash.CreateEmpty(),
                VersionHash.CreateEmpty(), null);

            Assert.Equal(StorageModStateEnum.Updating, storageMod.GetState());
            Assert.True((storageMod.GetMatchHash()).IsMatch(MatchHash.CreateEmpty()));
            Assert.True((storageMod.GetVersionHash()).IsMatch(VersionHash.CreateEmpty()));
        }

        [Fact]
        private async Task Prepared()
        {
            var target = new UpdateTarget("1234");
            var (implementation, storageMod, eventBus) = CreateStorageMod(target);
            eventBus.WorkUntil(100, () => storageMod.GetState() != StorageModStateEnum.Loading);


            var repo = CreateRepoMod();
            var update = storageMod.PrepareUpdate(repo, MatchHash.CreateEmpty(),
                VersionHash.CreateEmpty(), null);
            await update.Prepare(CancellationToken.None);

            Assert.Equal(StorageModStateEnum.Updating, storageMod.GetState());
            Assert.True((storageMod.GetMatchHash()).IsMatch(MatchHash.CreateEmpty()));
            Assert.True((storageMod.GetVersionHash()).IsMatch(VersionHash.CreateEmpty()));
        }

        [Fact]
        private async Task Updated()
        {
            var target = new UpdateTarget("123");
            var (implementation, storageMod, eventBus) = CreateStorageMod();

            var repo = CreateRepoMod();
            eventBus.WorkUntil(100, () => storageMod.GetState() != StorageModStateEnum.Loading);
            var update = storageMod.PrepareUpdate(repo, MatchHash.CreateEmpty(), VersionHash.CreateEmpty(), null);

            await update.Prepare(CancellationToken.None);
            await update.Update(CancellationToken.None);

            eventBus.WorkUntil(100, () => storageMod.GetState() == StorageModStateEnum.Created);

            Assert.Equal(StorageModStateEnum.Created, storageMod.GetState());
            Assert.True(storageMod.GetMatchHash().IsMatch(await MatchHash.CreateAsync(repo, CancellationToken.None)));
            Assert.True(storageMod.GetVersionHash().IsMatch(await VersionHash.CreateAsync(repo, CancellationToken.None)));
        }

        /*[Fact]
        private void UpdateAborted()
        {
            var (implementation, storageMod) = CreateStorageMod();

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
            var (implementation, storageMod) = CreateStorageMod(load: false);

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
        }*/
    }
}
