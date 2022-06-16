using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Ioc;
using BSU.Core.Model;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.Core.Tests.Mocks;
using BSU.Core.Tests.Util;
using BSU.CoreCommon;
using BSU.CoreCommon.Hashes;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests
{
    public class StorageModLifecycleTests : LoggedTest
    {
        public StorageModLifecycleTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }
        
        // TODO: use the load task
        private static StorageMod CreateStorageMod(int? match = null, int? version = null, UpdateTarget? stateTarget = null, Func<bool>? shouldThrow = null)
        {
            var mockStorage = new Mock<IStorageMod>(MockBehavior.Strict);
            mockStorage.Setup(m => m.GetTitle(It.IsAny<CancellationToken>())).Returns(Task.FromResult(""));
            var hashFuncs = new Dictionary<Type, Func<CancellationToken, Task<IModHash>>>();
            if (match != null)
                hashFuncs.Add(typeof(TestMatchHash), _ => Task.FromResult<IModHash>(new TestMatchHash((int)match)));
            if (version != null)
                hashFuncs.Add(typeof(TestVersionHash), _ => Task.FromResult<IModHash>(new TestVersionHash((int)version)));
            mockStorage.Setup(m => m.GetHashFunctions()).Returns(hashFuncs);

            mockStorage.Setup(m => m.GetFileList(It.IsAny<CancellationToken>())).Returns(() =>
            {
                if (shouldThrow?.Invoke() ?? false)
                    throw new TestException();
                return Task.FromResult(new List<string>());
            });

            var state = new Mock<IPersistedStorageModState>(MockBehavior.Strict);
            state.SetupProperty(x => x.UpdateTarget, stateTarget);

            var eventBus = new Mock<IDispatcher>(MockBehavior.Strict);
            eventBus.Setup(e => e.ExecuteSynchronized(It.IsAny<Action>())).Callback((Action a) => a());
            
            var serviceProvider = new ServiceProvider();
            serviceProvider.Add(eventBus.Object);
            serviceProvider.Add<IJobManager>(new TestJobManager());
            var storageMod = new StorageMod(mockStorage.Object, "mystorage", state.Object, null!, true, serviceProvider);

            return storageMod;
        }

        private static IRepositoryMod CreateRepoMod(int match, int version)
        {
            var mock = new Mock<IRepositoryMod>(MockBehavior.Strict);

            mock.Setup(m => m.GetFileList(It.IsAny<CancellationToken>())).Returns(Task.FromResult(new List<string>()));

            return mock.Object;
        }

        private static void CheckHashes(IHashCollection storageMod, int match, int version)
        {
            Assert.True(storageMod.GetHash(typeof(TestMatchHash)).Result.IsMatch(new TestMatchHash(match)));
            Assert.True(storageMod.GetHash(typeof(TestVersionHash)).Result.IsMatch(new TestVersionHash(version)));
        }

        private static UpdateTarget CreateUpdateTarget(int match, int version)
        {
            return new UpdateTarget(new HashCollection(
                new TestMatchHash(match),
                new TestVersionHash(version)
            ), "title");
        }

        [Fact]
        private void Created()
        {
            var storageMod = CreateStorageMod();
            Assert.Equal(StorageModStateEnum.Created, storageMod.GetState());
        }

        [Fact]
        private void CreatedWithTarget()
        {
            var storageMod = CreateStorageMod(stateTarget: CreateUpdateTarget(1, 1));

            Assert.Equal(StorageModStateEnum.CreatedWithUpdateTarget, storageMod.GetState());
            CheckHashes(storageMod, 1, 1);
        }

        [Fact]
        private void UpdateWithTarget()
        {
            var storageMod = CreateStorageMod(stateTarget: CreateUpdateTarget(1, 1));

            var repo = CreateRepoMod(2, 2);
            storageMod.Update(repo, CreateUpdateTarget(2, 2), null, CancellationToken.None);
            // TODO: wait for preparation, but not actual update?

            Assert.Equal(StorageModStateEnum.Updating, storageMod.GetState());
            CheckHashes(storageMod, 2, 2);
        }

        [Fact]
        private async Task Updated()
        {
            var storageMod = CreateStorageMod();

            var repo = CreateRepoMod(2, 2);
            var update = storageMod.Update(repo, CreateUpdateTarget(2, 2), null, CancellationToken.None);
            await update;

            Assert.Equal(StorageModStateEnum.Created, storageMod.GetState());
            CheckHashes(storageMod, 2, 2);
        }

        [Fact]
        private async Task UpdateAborted()
        {
            var storageMod = CreateStorageMod();

            var repo = CreateRepoMod(2, 2);

            var cts = new CancellationTokenSource();

            var update = storageMod.Update(repo, CreateUpdateTarget(2, 2), null, cts.Token);

            cts.Cancel();
            await update;

            Assert.Equal(StorageModStateEnum.CreatedWithUpdateTarget, storageMod.GetState());
            CheckHashes(storageMod, 2, 2);
        }

        [Fact]
        private void ErrorLoad()
        {
            var storageMod = CreateStorageMod(shouldThrow: () => true);
            
            Assert.Equal(StorageModStateEnum.Error, storageMod.GetState());
        }

        [Fact]
        private async Task ErrorUpdate()
        {
            var shouldThrow = false;
            var storageMod = CreateStorageMod(shouldThrow: () => shouldThrow);

            var repo = CreateRepoMod(2, 2);
            
            shouldThrow = true;

            var update = storageMod.Update(repo, CreateUpdateTarget(2, 2), null, CancellationToken.None);

            var result = await update;

            Assert.Equal(UpdateResult.Failed, result);
            CheckHashes(storageMod, 2, 2);
        }
    }
}
