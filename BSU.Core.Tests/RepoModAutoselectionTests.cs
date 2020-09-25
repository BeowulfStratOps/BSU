using System;
using BSU.Core.Model;
using BSU.Core.Persistence;
using BSU.Core.Tests.Mocks;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests
{
    public class RepoModAutoselectionTests : LoggedTest
    {
        public RepoModAutoselectionTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        private void DontAutoselectOverSelection_Mod()
        {
            var worker = new MockWorker();

            var storageMod = new Mock<IModelStorageMod>(MockBehavior.Strict);
            storageMod.Setup(s => s.GetStorageModIdentifiers()).Returns(new PersistedSelection(Guid.Empty, "1"));

            var storageMod2 = new Mock<IModelStorageMod>(MockBehavior.Strict);
            storageMod.Setup(s => s.GetStorageModIdentifiers()).Returns(new PersistedSelection(Guid.Empty, "2"));

                var state = new MockRepositoryModState();
            var repoMod = new RepositoryMod(worker, new MockRepositoryMod(), "asdf", worker,
                state, new RelatedActionsBag(), new MockModelStructure());
            worker.DoWork();
            repoMod.ChangeAction(storageMod.Object, ModActionEnum.Update);
            repoMod.Selection = new RepositoryModActionSelection(storageMod.Object);
            repoMod.ChangeAction(storageMod2.Object, ModActionEnum.Use);
            repoMod.AllModsLoaded = true;

            Assert.Equal(repoMod.Selection, new RepositoryModActionSelection(storageMod.Object));
        }

        [Fact]
        private void DontAutoselectOverSelection_Download()
        {
            var worker = new MockWorker();

            var storage = new Mock<IModelStorage>(MockBehavior.Strict);
            storage.Setup(s => s.GetStorageIdentifier()).Returns(new PersistedSelection(Guid.Empty, null));

            var storageMod2 = new Mock<IModelStorageMod>(MockBehavior.Strict);

            var state = new MockRepositoryModState();
            var repoMod = new RepositoryMod(worker, new MockRepositoryMod(), "asdf", worker,
                state, new RelatedActionsBag(), new MockModelStructure());
            worker.DoWork();
            repoMod.Selection = new RepositoryModActionSelection(storage.Object);
            repoMod.ChangeAction(storageMod2.Object, ModActionEnum.Use);
            repoMod.AllModsLoaded = true;

            Assert.Equal(repoMod.Selection, new RepositoryModActionSelection(storage.Object));
        }

        [Fact]
        private void DontAutoselectOverSelection_DoNothing()
        {
            var worker = new MockWorker();

            var storageMod2 = new Mock<IModelStorageMod>(MockBehavior.Strict);

            var state = new MockRepositoryModState();
            var repoMod = new RepositoryMod(worker, new MockRepositoryMod(), "asdf", worker,
                state, new RelatedActionsBag(), new MockModelStructure());
            worker.DoWork();
            repoMod.Selection = new RepositoryModActionSelection();
            repoMod.ChangeAction(storageMod2.Object, ModActionEnum.Use);
            repoMod.AllModsLoaded = true;

            Assert.Equal(repoMod.Selection, new RepositoryModActionSelection());
        }

        [Fact]
        private void OnlySelectWhenAllLoaded()
        {
            var worker = new MockWorker();
            var storageMod = new Mock<IModelStorageMod>(MockBehavior.Strict);
            storageMod.Setup(s => s.GetStorageModIdentifiers()).Returns(new PersistedSelection(Guid.Empty, "1"));

            var state = new MockRepositoryModState();
            var repoMod = new RepositoryMod(worker, new MockRepositoryMod(), "asdf", worker,
                state, new RelatedActionsBag(), new MockModelStructure());
            worker.DoWork();
            repoMod.ChangeAction(storageMod.Object, ModActionEnum.Use);
            Assert.Null(repoMod.Selection);
            repoMod.AllModsLoaded = true;
            Assert.Equal(repoMod.Selection, new RepositoryModActionSelection(storageMod.Object));
        }

        [Fact]
        private void SelectUsedModBeforeAllLoaded()
        {
            var worker = new MockWorker();

            var modIdentifier = new PersistedSelection(Guid.Empty, "qwer");

            var storageMod = new Mock<IModelStorageMod>(MockBehavior.Strict);
            storageMod.Setup(s => s.GetStorageModIdentifiers()).Returns(modIdentifier);

            var state = new MockRepositoryModState {Selection = modIdentifier};
            var repoMod = new RepositoryMod(worker, new MockRepositoryMod(), "asdf", worker,
                state, new RelatedActionsBag(), new MockModelStructure());
            worker.DoWork();
            repoMod.ChangeAction(storageMod.Object, ModActionEnum.Use);
            Assert.Equal(repoMod.Selection, new RepositoryModActionSelection(storageMod.Object));
            repoMod.AllModsLoaded = true;
            Assert.Equal(repoMod.Selection, new RepositoryModActionSelection(storageMod.Object));
        }

        [Fact]
        private void SelectAfterAllLoaded()
        {
            var worker = new MockWorker();
            var storageMod = new Mock<IModelStorageMod>(MockBehavior.Strict);
            storageMod.Setup(s => s.GetStorageModIdentifiers()).Returns(new PersistedSelection(Guid.Empty, "1"));

            var state = new MockRepositoryModState();
            var repoMod = new RepositoryMod(worker, new MockRepositoryMod(), "asdf", worker,
                state, new RelatedActionsBag(), new MockModelStructure());
            worker.DoWork();
            repoMod.AllModsLoaded = true;
            repoMod.ChangeAction(storageMod.Object, ModActionEnum.Use);
            Assert.Equal(repoMod.Selection, new RepositoryModActionSelection(storageMod.Object));
        }

        [Fact]
        private void ToggleAllLoaded()
        {
            var worker = new MockWorker();

            var storageMod = new Mock<IModelStorageMod>(MockBehavior.Strict);
            storageMod.Setup(s => s.GetStorageModIdentifiers()).Returns(new PersistedSelection(Guid.Empty, "1"));

            var storageMod2 = new Mock<IModelStorageMod>(MockBehavior.Strict);
            storageMod2.Setup(s => s.GetStorageModIdentifiers()).Returns(new PersistedSelection(Guid.Empty, "2"));

            var state = new MockRepositoryModState();
            var repoMod = new RepositoryMod(worker, new MockRepositoryMod(), "asdf", worker,
                state, new RelatedActionsBag(), new MockModelStructure());
            worker.DoWork();
            repoMod.AllModsLoaded = true;
            repoMod.ChangeAction(storageMod.Object, ModActionEnum.Use);
            Assert.Equal(repoMod.Selection, new RepositoryModActionSelection(storageMod.Object));
            repoMod.AllModsLoaded = false;
            repoMod.ChangeAction(storageMod2.Object, ModActionEnum.Use);
            Assert.Equal(repoMod.Selection, new RepositoryModActionSelection(storageMod.Object));
            repoMod.AllModsLoaded = true;
            Assert.Equal(repoMod.Selection, new RepositoryModActionSelection(storageMod.Object));
        }

        [Fact]
        private void DontSelectWhileModsAreLoading()
        {
            var worker = new MockWorker();

            var storageMod = new Mock<IModelStorageMod>(MockBehavior.Strict);
            storageMod.Setup(s => s.GetStorageModIdentifiers()).Returns(new PersistedSelection(Guid.Empty, "1"));

            var structure = new MockModelStructure();
            var storage = new Mock<IModelStorage>();
            structure.Storages.Add(storage.Object);
            var state = new MockRepositoryModState();
            var repoMod = new RepositoryMod(worker, new MockRepositoryMod(), "asdf", worker,
                state, new RelatedActionsBag(), structure);
            worker.DoWork();
            repoMod.ChangeAction(storageMod.Object, ModActionEnum.Loading);
            repoMod.AllModsLoaded = true;
            Assert.Null(repoMod.Selection);
            repoMod.ChangeAction(storageMod.Object, ModActionEnum.Use);
            Assert.Equal(repoMod.Selection, new RepositoryModActionSelection(storageMod.Object));
        }
    }
}
