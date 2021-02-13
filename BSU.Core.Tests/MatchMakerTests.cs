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
        }/*

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

        private ModActionEnum? DoCheck(IRepositoryModState repoState, IStorageModState storageState, bool canWrite = true)
        {
            var repoMod = new Mock<IModelRepositoryMod>(MockBehavior.Strict);
            var storageMod = new Mock<IModelStorageMod>(MockBehavior.Strict);

            var changes = new List<ModActionEnum?>();

            repoMod.Setup(r => r.GetState()).Returns(repoState);
            
            repoMod.Setup(s => s.Identifier).Returns("repoMod");

            storageMod.Setup(s => s.GetState()).Returns(storageState);

            storageMod.Setup(s => s.RequireHash());

            storageMod.Setup(s => s.CanWrite).Returns(canWrite);

            storageMod.Setup(s => s.Identifier).Returns("storageMod");

            var structure = new MockModelStructure();
            var matchMaker = new MatchMaker(structure);

            // Do storage first, to not have to mock AllLoaded check
            structure.StorageMods.Add(storageMod.Object);
            matchMaker.AddStorageMod(storageMod.Object);

            structure.RepositoryMods.Add(repoMod.Object);
            matchMaker.AddRepositoryMod(repoMod.Object);

            Assert.True(changes.Count() <= 1);

            return (storageMod, changes.FirstOrDefault(), changes.Any());

            return CoreCalculation.GetModAction().Result;
        }

        [Fact]
        private void NoMatch()
        {
            var repoState = new RepositoryModState(GetMatchHash("1"), GetVersionHash("1"), null);

            var storageState =
                new StorageModState(GetMatchHash("2"), null, null, null, StorageModStateEnum.Created, null);

            var action = DoCheck(repoState, storageState);

            Assert.Null(action);
        }

        [Fact]
        private void Use()
        {
            var repoState = new RepositoryModState(GetMatchHash("1"), GetVersionHash("1"), null);

            var storageState =
                new StorageModState(GetMatchHash("1"), GetVersionHash("1"), null, null, StorageModStateEnum.Created, null);

            var (storageMod, action, wasCalled) = DoCheck(repoState, storageState);

            Assert.True(wasCalled);
            Assert.Equal(ModActionEnum.Use, action);
        }

        [Fact]
        private void Update()
        {
            var repoState = new RepositoryModState(GetMatchHash("1"), GetVersionHash("2"), null);

            var storageState =
                new StorageModState(GetMatchHash("1"), GetVersionHash("1"), null, null, StorageModStateEnum.Created, null);

            var (storageMod, action, wasCalled) = DoCheck(repoState, storageState);

            Assert.True(wasCalled);
            Assert.Equal(ModActionEnum.Update, action);
        }

        [Fact]
        private void Error()
        {
            var repoState = new RepositoryModState(GetMatchHash("1"), GetVersionHash("1"), null);

            var storageState =
                new StorageModState(null, null, null, null, StorageModStateEnum.Error, null);

            var (storageMod, action, wasCalled) = DoCheck(repoState, storageState);
            Assert.False(wasCalled);
        }

        [Fact]
        private void ContinueUpdate()
        {
            var repoState = new RepositoryModState(GetMatchHash("1"), GetVersionHash("2"), null);

            var updateTarget = new UpdateTarget(repoState.VersionHash.GetHashString(), "asdf");

            var storageState =
                new StorageModState(GetMatchHash("1"), GetVersionHash("3"), updateTarget, null, StorageModStateEnum.CreatedWithUpdateTarget, null);

            var (storageMod, action, wasCalled) = DoCheck(repoState, storageState);
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
            Assert.True(wasCalled);
            Assert.Equal(ModActionEnum.Await, action);
        }

        [Fact]
        private void AbortAndUpdate()
        {
            // TODO: this requires some better match-hash lifecycle. like downloading the relevant files and creating the expected match-hash before the remainder of the update is done 
            var repoState = new RepositoryModState(GetMatchHash("1"), GetVersionHash("2"), null);

            var updateTarget = new UpdateTarget(GetVersionHash("4").GetHashString(), "asdf");

            var storageState =
                new StorageModState(GetMatchHash("1"), GetVersionHash("3"), updateTarget, updateTarget, StorageModStateEnum.Updating, null);

            var (storageMod, action, wasCalled) = DoCheck(repoState, storageState);
            Assert.True(wasCalled);
            Assert.Equal(ModActionEnum.AbortAndUpdate, action);
        }*/
    }
}
