using System.Linq;
using BSU.Core.Model;
using BSU.Core.Tests.Mocks;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests
{
    public class ConflictTests : LoggedTest
    {
        public ConflictTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }
        
        [Fact]
        private void HasConflict()
        {
            var relatedActionsBag = new RelatedActionsBag();
            var worker = new MockWorker();
            
            var mockRepo = new MockRepositoryMod();
            var mockRepoState = new MockRepositoryModState();
            IModelRepositoryMod repoMod = new RepositoryMod(worker, mockRepo, "asdf", worker, mockRepoState, relatedActionsBag, null);
            
            var mockRepo2 = new MockRepositoryMod();
            mockRepo2.SetFile("asdf", "qwer");
            var mockRepoState2 = new MockRepositoryModState();
            IModelRepositoryMod repoMod2 = new RepositoryMod(worker, mockRepo2, "asdf", worker, mockRepoState2, relatedActionsBag, null);
            
            worker.DoWork();
            
            IModelStorageMod storageMod = new MockModelStorageMod(); // TODO: use mocking?
            
            repoMod.ChangeAction(storageMod, ModActionEnum.Update);
            repoMod2.ChangeAction(storageMod, ModActionEnum.Update);

            var action1 = repoMod.Actions[storageMod];
            var action2 = repoMod2.Actions[storageMod];
            
            Assert.Single(action1.Conflicts);
            Assert.Single(action2.Conflicts);
            
            Assert.Equal(action1, action2.Conflicts.Single());
            Assert.Equal(action2, action1.Conflicts.Single());
        }
        
        [Fact]
        private void NoConflict()
        {
            var relatedActionsBag = new RelatedActionsBag();
            var worker = new MockWorker();
            
            var mockRepo = new MockRepositoryMod();
            mockRepo.SetFile("asdf", "qwer");
            var mockRepoState = new MockRepositoryModState();
            IModelRepositoryMod repoMod = new RepositoryMod(worker, mockRepo, "asdf", worker, mockRepoState, relatedActionsBag, null);
            
            var mockRepo2 = new MockRepositoryMod();
            mockRepo2.SetFile("asdf", "qwer");
            var mockRepoState2 = new MockRepositoryModState();
            IModelRepositoryMod repoMod2 = new RepositoryMod(worker, mockRepo2, "asdf", worker, mockRepoState2, relatedActionsBag, null);
            
            worker.DoWork();
            
            IModelStorageMod storageMod = new MockModelStorageMod(); // create instance
            
            repoMod.ChangeAction(storageMod, ModActionEnum.Update);
            repoMod2.ChangeAction(storageMod, ModActionEnum.Update);

            var action1 = repoMod.Actions[storageMod];
            var action2 = repoMod2.Actions[storageMod];
            
            Assert.Empty(action1.Conflicts);
            Assert.Empty(action2.Conflicts);
        }
    }
}