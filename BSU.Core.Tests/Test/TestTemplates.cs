using System.Collections.Generic;
using System.Linq;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.Core.Persistence;
using BSU.Core.Tests.Mocks;
using BSU.CoreCommon;
using Xunit;

namespace BSU.Core.Tests.Test
{
    /*
want:
 storage mod lifecycle
 matchmaker loading/hashing cycle, actions
conflicts from actions
autoselect from actions with conflicts
repo state from selections

plan:
enqueue interface
irepomod, istorage mod / simplify creation

     */
    public class TestTemplates
    {
        [Fact]
        private void StorageModLifecycle()
        {
            var implementation = new MockStorageMod();
            var internalState = new MockStorageModState();
            var worker = new MockWorker();
            var mod = new StorageMod(worker, implementation, "asdf", null, internalState, worker, "qwer", true);
        }

        [Fact]
        private void MatchmakerLoadingHashingCycle()
        {
            IModelRepositoryMod repoMod = new MockModelRepositoryMod(); // create instance
            IModelStorageMod storageMod = new MockModelStorageMod(); // create instance
            var matchMaker = new MatchMaker(null);
            matchMaker.AddRepositoryMod(repoMod);
            matchMaker.AddStorageMod(storageMod);
            // do things
            // check repoMod.Actions  // TODO: bit meh
        }
        
        [Fact]
        private void Conflicts()
        {
            var relatedActionsBag = new RelatedActionsBag();
            var worker = new MockWorker();
            
            var mockRepo = new MockRepositoryMod();
            var mockRepoState = new MockRepositoryModState();
            IModelRepositoryMod repoMod = new RepositoryMod(worker, mockRepo, "asdf", worker, mockRepoState, relatedActionsBag, null);
            
            var mockRepo2 = new MockRepositoryMod();
            var mockRepoState2 = new MockRepositoryModState();
            IModelRepositoryMod repoMod2 = new RepositoryMod(worker, mockRepo, "asdf", worker, mockRepoState, relatedActionsBag, null);
            
            IModelStorageMod storageMod = new MockModelStorageMod(); // create instance
            
            repoMod.ChangeAction(storageMod, ModActionEnum.Update);
            repoMod2.ChangeAction(storageMod, ModActionEnum.Update);
            
            // TODO: check stuff
        }
        
        [Fact]
        private void Autoselect()
        {
            var modAction = new ModAction(ModActionEnum.Await, null, new VersionHash(new byte[] { }),
                new HashSet<ModAction>()); 
            var result = CoreCalculation.AutoSelect(true, new Dictionary<IModelStorageMod, ModAction>(), null,
                new StorageModIdentifiers("", ""));
        }
        
        [Fact]
        private void RepoState()
        {
            var result = CoreCalculation.CalculateRepositoryState(new List<IModelRepositoryMod>
            {
                null // create instance
            });
        }
    }
}