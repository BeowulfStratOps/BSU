using System.Collections.Generic;
using System.Linq;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.Core.Tests;
using BSU.Core.Tests.Mocks;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests
{
    public class MatchMakerTests : LoggedTest
    {
        public MatchMakerTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        private static MatchHash GetMatchHash(string match)
        {
            var mockRepo = new MockRepositoryMod();
            for (int i = 0; i < 3; i++)
            {
                mockRepo.SetFile($"/addons/{match}_{i}.pbo", i.ToString());                
            }
            return new MatchHash(mockRepo);
        }

        private static VersionHash GetVersionHash(string version)
        {
            var mockRepo = new MockRepositoryMod();
            for (int i = 0; i < 3; i++)
            {
                mockRepo.SetFile($"/addons/file_{i}.pbo", version + i);                
            }
            return new VersionHash(mockRepo);
        }

        private (Mock<IModelStorageMod>, ModActionEnum?, bool wasCalled) DoCheck(RepositoryModState repoState, StorageModState storageState, bool canWrite = true)
        {
            var repoMod = new Mock<IModelRepositoryMod>(MockBehavior.Strict);
            var storageMod = new Mock<IModelStorageMod>(MockBehavior.Strict);
            
            var changes = new List<ModActionEnum?>();

            repoMod.Setup(r => r.GetState()).Returns(repoState);

            storageMod.Setup(s => s.GetState()).Returns(storageState);

            storageMod.Setup(s => s.RequireHash());

            storageMod.Setup(s => s.CanWrite).Returns(canWrite);

            repoMod.Setup(r => r.ChangeAction(storageMod.Object, It.IsAny<ModActionEnum?>())).Callback<IModelStorageMod, ModActionEnum?>(
                (_, state) =>
                {
                    changes.Add(state);
                });
            
            var structure = new MockModelStructure();
            var matchMaker = new MatchMaker(structure);
            
            // Do storage first, to not have to mock AllLoaded check
            structure.StorageMods.Add(storageMod.Object);
            matchMaker.AddStorageMod(storageMod.Object);
            
            structure.RepositoryMods.Add(repoMod.Object);
            matchMaker.AddRepositoryMod(repoMod.Object);
            
            Assert.True(changes.Count() <= 1);

            return (storageMod, changes.FirstOrDefault(), changes.Any());
        }
        
        [Fact]
        private void RepoLoading()
        {
            var repoState = new RepositoryModState(null, null, null);

            var storageState =
                new StorageModState(GetMatchHash("1"), null, null, null, StorageModStateEnum.Loaded, null);

            var (storageMod, action, wasCalled) = DoCheck(repoState, storageState);
            
            storageMod.Verify(s => s.RequireHash(), Times.Never);
            Assert.False(wasCalled);
        }
        
        [Fact]
        private void StorageLoading()
        {
            var repoState = new RepositoryModState(GetMatchHash("1"), GetVersionHash("1"), null);

            var storageState =
                new StorageModState(null, null, null, null, StorageModStateEnum.Loading, null);

            var (storageMod, action, wasCalled) = DoCheck(repoState, storageState);
            
            storageMod.Verify(s => s.RequireHash(), Times.Never);
            Assert.False(wasCalled);
        }

        [Fact]
        private void StorageLoaded_Match()
        {
            var repoState = new RepositoryModState(GetMatchHash("1"), GetVersionHash("1"), null);

            var storageState =
                new StorageModState(GetMatchHash("1"), null, null, null, StorageModStateEnum.Loaded, null);

            var (storageMod, action, wasCalled) = DoCheck(repoState, storageState);
            
            storageMod.Verify(s => s.RequireHash(), Times.Once);
            Assert.True(wasCalled);
            Assert.Equal(ModActionEnum.Loading, action);
        }
        
        [Fact]
        private void StorageLoaded_NoMatch()
        {
            var repoState = new RepositoryModState(GetMatchHash("1"), GetVersionHash("1"), null);

            var storageState =
                new StorageModState(GetMatchHash("2"), null, null, null, StorageModStateEnum.Loaded, null);

            var (storageMod, action, wasCalled) = DoCheck(repoState, storageState);
            
            storageMod.Verify(s => s.RequireHash(), Times.Never);
            Assert.False(wasCalled);
        }
        
        [Fact]
        private void StorageHashing()
        {
            var repoState = new RepositoryModState(GetMatchHash("1"), GetVersionHash("1"), null);

            var storageState =
                new StorageModState(GetMatchHash("1"), null, null, null, StorageModStateEnum.Hashing, null);

            var (storageMod, action, wasCalled) = DoCheck(repoState, storageState);
            
            storageMod.Verify(s => s.RequireHash(), Times.Never);
            Assert.True(wasCalled);
            Assert.Equal(ModActionEnum.Loading, action);
        }
        
        [Fact]
        private void Hashed_Use()
        {
            var repoState = new RepositoryModState(GetMatchHash("1"), GetVersionHash("1"), null);

            var storageState =
                new StorageModState(GetMatchHash("1"), GetVersionHash("1"), null, null, StorageModStateEnum.Hashed, null);

            var (storageMod, action, wasCalled) = DoCheck(repoState, storageState);
            
            storageMod.Verify(s => s.RequireHash(), Times.Never);
            Assert.True(wasCalled);
            Assert.Equal(ModActionEnum.Use, action);
        }
        
        [Fact]
        private void Hashed_Update()
        {
            var repoState = new RepositoryModState(GetMatchHash("1"), GetVersionHash("2"), null);

            var storageState =
                new StorageModState(GetMatchHash("1"), GetVersionHash("1"), null, null, StorageModStateEnum.Hashed, null);

            var (storageMod, action, wasCalled) = DoCheck(repoState, storageState);
            
            storageMod.Verify(s => s.RequireHash(), Times.Never);
            Assert.True(wasCalled);
            Assert.Equal(ModActionEnum.Update, action);
        }
        
        [Fact]
        private void ErrorLoad()
        {
            var repoState = new RepositoryModState(GetMatchHash("1"), GetVersionHash("1"), null);

            var storageState =
                new StorageModState(null, null, null, null, StorageModStateEnum.ErrorLoad, null);

            var (storageMod, action, wasCalled) = DoCheck(repoState, storageState);
            
            storageMod.Verify(s => s.RequireHash(), Times.Never);
            Assert.False(wasCalled);
        }
        
        [Fact]
        private void ErroUpdate()
        {
            var repoState = new RepositoryModState(GetMatchHash("1"), GetVersionHash("2"), null);

            var updateTarget = new UpdateTarget(repoState.VersionHash.GetHashString(), "asdf");
            
            var storageState =
                new StorageModState(GetMatchHash("1"), null, updateTarget, null, StorageModStateEnum.ErrorUpdate, null);

            var (storageMod, action, wasCalled) = DoCheck(repoState, storageState);
            
            storageMod.Verify(s => s.RequireHash(), Times.Never);
            Assert.True(wasCalled);
            Assert.Equal(ModActionEnum.Error, action);
        }
        
        [Fact]
        private void ContinueUpdate()
        {
            var repoState = new RepositoryModState(GetMatchHash("1"), GetVersionHash("2"), null);

            var updateTarget = new UpdateTarget(repoState.VersionHash.GetHashString(), "asdf");
            
            var storageState =
                new StorageModState(GetMatchHash("1"), GetVersionHash("3"), updateTarget, null, StorageModStateEnum.CreatedWithUpdateTarget, null);

            var (storageMod, action, wasCalled) = DoCheck(repoState, storageState);
            
            storageMod.Verify(s => s.RequireHash(), Times.Never);
            Assert.True(wasCalled);
            Assert.Equal(ModActionEnum.ContinueUpdate, action);
        }
        
        [Fact]
        private void ContinueDownload()
        {
            var repoState = new RepositoryModState(GetMatchHash("1"), GetVersionHash("2"), null);

            var updateTarget = new UpdateTarget(repoState.VersionHash.GetHashString(), "asdf");
            
            var storageState =
                new StorageModState(GetMatchHash("1"), GetVersionHash("3"), updateTarget, null, StorageModStateEnum.CreatedForDownload, null);

            var (storageMod, action, wasCalled) = DoCheck(repoState, storageState);
            
            storageMod.Verify(s => s.RequireHash(), Times.Never);
            Assert.True(wasCalled);
            Assert.Equal(ModActionEnum.ContinueUpdate, action);
        }
        
        [Fact]
        private void Await()
        {
            var repoState = new RepositoryModState(GetMatchHash("1"), GetVersionHash("2"), null);

            var updateTarget = new UpdateTarget(repoState.VersionHash.GetHashString(), "asdf");
            
            var storageState =
                new StorageModState(GetMatchHash("1"), GetVersionHash("3"), updateTarget, updateTarget, StorageModStateEnum.Updating, null);

            var (storageMod, action, wasCalled) = DoCheck(repoState, storageState);
            
            storageMod.Verify(s => s.RequireHash(), Times.Never);
            Assert.True(wasCalled);
            Assert.Equal(ModActionEnum.Await, action);
        }
        
        [Fact]
        private void AbortAndUpdate()
        {
            var repoState = new RepositoryModState(GetMatchHash("1"), GetVersionHash("2"), null);

            var updateTarget = new UpdateTarget(GetVersionHash("4").GetHashString(), "asdf");
            
            var storageState =
                new StorageModState(GetMatchHash("1"), GetVersionHash("3"), updateTarget, updateTarget, StorageModStateEnum.Updating, null);

            var (storageMod, action, wasCalled) = DoCheck(repoState, storageState);
            
            storageMod.Verify(s => s.RequireHash(), Times.Never);
            Assert.True(wasCalled);
            Assert.Equal(ModActionEnum.AbortAndUpdate, action);
        }
        
        [Fact]
        private void Unusable()
        {
            var repoState = new RepositoryModState(GetMatchHash("1"), GetVersionHash("2"), null);
            
            var storageState =
                new StorageModState(GetMatchHash("1"), GetVersionHash("1"), null, null, StorageModStateEnum.Hashed, null);

            var (storageMod, action, wasCalled) = DoCheck(repoState, storageState, false);
            
            storageMod.Verify(s => s.RequireHash(), Times.Never);
            Assert.True(wasCalled);
            Assert.Equal(ModActionEnum.Unusable, action);
        }
    }
}