using System.Linq;
using BSU.Core.Hashes;
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
            
            var bag = relatedActionsBag.GetBag(new object());
            
            var action1 = new ModAction(ModActionEnum.Update, null, new VersionHash(new byte[]{1}), bag);
            var action2 = new ModAction(ModActionEnum.Update, null, new VersionHash(new byte[]{2}), bag);
            
            Assert.Single(action1.Conflicts);
            Assert.Single(action2.Conflicts);
            
            Assert.Equal(action1, action2.Conflicts.Single());
            Assert.Equal(action2, action1.Conflicts.Single());
        }
        
        [Fact]
        private void NoConflict()
        {
            var relatedActionsBag = new RelatedActionsBag();
            
            IModelStorageMod storageMod = new MockModelStorageMod();
            var bag = relatedActionsBag.GetBag(new object());
            
            var action1 = new ModAction(ModActionEnum.Update, null, new VersionHash(new byte[]{1}), bag);
            var action2 = new ModAction(ModActionEnum.Update, null, new VersionHash(new byte[]{1}), bag);
            
            Assert.Empty(action1.Conflicts);
            Assert.Empty(action2.Conflicts);
        }
    }
}