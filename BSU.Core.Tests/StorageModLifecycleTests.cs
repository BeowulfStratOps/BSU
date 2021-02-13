using System;
using System.Linq;
using System.Threading.Tasks;
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

            Assert.Equal(StorageModStateEnum.Created, storageMod.GetState());
            Assert.NotNull(storageMod.GetMatchHash().Result); // TODO: check exact value
            Assert.NotNull(storageMod.GetVersionHash().Result); // TODO: check exact value
        }

        [Fact]
        private void CreatedWithTarget()
        {
            var target = new UpdateTarget("1234", "LeMod");
            var (implementation, storageMod, worker) = CreateStorageMod(target);

            Assert.Equal(StorageModStateEnum.CreatedWithUpdateTarget, storageMod.GetState());
            //Assert.NotNull(storageMod.GetMatchHash().Result); // TODO: check exact value
            Assert.NotNull(storageMod.GetVersionHash().Result); // TODO: check exact value
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
            Assert.NotNull(storageMod.GetMatchHash().Result); // TODO: check exact value
            Assert.NotNull(storageMod.GetVersionHash().Result); // TODO: check exact value
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
            Assert.NotNull(storageMod.GetMatchHash().Result); // TODO: check exact value
            Assert.NotNull(storageMod.GetVersionHash().Result); // TODO: check exact value
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
            Assert.NotNull(storageMod.GetMatchHash().Result); // TODO: check exact value
            Assert.NotNull(storageMod.GetVersionHash().Result); // TODO: check exact value
        }

        [Fact]
        private void ErrorLoad()
        {
            var (implementation, storageMod, worker) = CreateStorageMod();

            implementation.ThrowErrorLoad = true;

            try
            {
                storageMod.GetMatchHash().Wait();
                Assert.False(true);
            }
            catch (AggregateException e)
            {
                if (!(e.InnerExceptions.Single() is TestException)) throw;
            }

            Assert.Equal(StorageModStateEnum.Error, storageMod.GetState());
        }

        [Fact]
        private void ErrorUpdateDo()
        {
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
            Assert.NotNull(storageMod.GetMatchHash().Result);
            Assert.NotNull(storageMod.GetVersionHash().Result);
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
            Assert.NotNull(storageMod.GetMatchHash().Result);
            Assert.NotNull(storageMod.GetVersionHash().Result);
        }
    }
}
