using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Ioc;
using BSU.Core.Model;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.Core.Sync;
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
        
        // TODO: what should happen if a hash fails?
        private static StorageMod CreateStorageMod(int? match = null, int? version = null,
            UpdateTarget? stateTarget = null, bool loadingShouldThrow = false, Func<Task<UpdateResult>>? updateFunc = null)
        {
            var mockStorage = new Mock<IStorageMod>(MockBehavior.Strict);

            mockStorage.Setup(m => m.GetFileList(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new List<string>()));
            
            if (loadingShouldThrow)
                mockStorage.Setup(m => m.GetTitle(It.IsAny<CancellationToken>())).Returns(Task.FromException<string>(new TestException()));
            else
                mockStorage.Setup(m => m.GetTitle(It.IsAny<CancellationToken>())).Returns(Task.FromResult(""));
            
            
            var hashFuncs = new Dictionary<Type, Func<CancellationToken, Task<IModHash>>>();
            if (match != null)
                hashFuncs.Add(typeof(TestMatchHash), _ => Task.FromResult((IModHash)new TestMatchHash((int)match)));
            if (version != null)
                hashFuncs.Add(typeof(TestVersionHash), _ => Task.FromResult((IModHash)new TestVersionHash((int)version)));
            mockStorage.Setup(m => m.GetHashFunctions()).Returns(hashFuncs);

            var state = new Mock<IPersistedStorageModState>(MockBehavior.Strict);
            state.SetupProperty(x => x.UpdateTarget, stateTarget);

            var eventBus = new Mock<IDispatcher>(MockBehavior.Strict);
            eventBus.Setup(e => e.ExecuteSynchronized(It.IsAny<Action>())).Callback((Action a) => a());
            
            var serviceProvider = new ServiceProvider();
            serviceProvider.Add(eventBus.Object);
            serviceProvider.Add<IJobManager>(new TestJobManager());
            var updateService = new Mock<IUpdateService>(MockBehavior.Strict);

            if (updateFunc != null)
                updateService.Setup(u => u.UpdateAsync(It.IsAny<IRepositoryMod>(), It.IsAny<IStorageMod>(),
                    It.IsAny<CancellationToken>(), It.IsAny<IProgress<FileSyncStats>?>())).Returns(updateFunc);
            
            serviceProvider.Add(updateService.Object);
            var storageMod = new StorageMod(mockStorage.Object, "mystorage", state.Object, null!, true, serviceProvider);

            return storageMod;
        }

        private static IRepositoryMod CreateRepoMod()
        {
            var mock = new Mock<IRepositoryMod>(MockBehavior.Strict);
            return mock.Object;
        }

        private static void CheckHashes(IHashCollection storageMod, int match, int version)
        {
            Assert.True(storageMod.GetHash(typeof(TestMatchHash)).Result.IsMatch(new TestMatchHash(match)));
            Assert.True(storageMod.GetHash(typeof(TestVersionHash)).Result.IsMatch(new TestVersionHash(version)));
        }

        private static UpdateTarget CreateUpdateTarget(int match, int version)
        {
            return new UpdateTarget(new List<IModHash>
            {
                new TestMatchHash(match),
                new TestVersionHash(version)
            }, "title");
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
        private async Task UpdateWithTarget()
        {
            var tcs = new TaskCompletionSource<UpdateResult>();
            var storageMod = CreateStorageMod(stateTarget: CreateUpdateTarget(1, 1), updateFunc: () => tcs.Task);

            var repo = CreateRepoMod();
            var update = storageMod.Update(repo, CreateUpdateTarget(2, 2), null, CancellationToken.None);

            Assert.Equal(StorageModStateEnum.Updating, storageMod.GetState());
            CheckHashes(storageMod, 2, 2);

            tcs.SetResult(UpdateResult.Success);
            await update;
        }

        [Fact]
        private async Task Updated()
        {
            var storageMod = CreateStorageMod(updateFunc: () => Task.FromResult(UpdateResult.Success));

            var repo = CreateRepoMod();
            await storageMod.Update(repo, CreateUpdateTarget(2, 2), null, CancellationToken.None);

            Assert.Equal(StorageModStateEnum.Created, storageMod.GetState());
            CheckHashes(storageMod, 2, 2);
        }

        [Fact]
        private void ErrorLoad()
        {
            var storageMod = CreateStorageMod(match: 1, loadingShouldThrow: true);
            
            Assert.Equal(StorageModStateEnum.Error, storageMod.GetState());
        }

        [Fact]
        private async Task ErrorUpdate()
        {
            var storageMod = CreateStorageMod(updateFunc: () => Task.FromResult(UpdateResult.Failed));

            var repo = CreateRepoMod();

            await storageMod.Update(repo, CreateUpdateTarget(2, 2), null, CancellationToken.None);
            
            Assert.Equal(StorageModStateEnum.CreatedWithUpdateTarget, storageMod.GetState());

            CheckHashes(storageMod, 2, 2);
        }
    }
}
