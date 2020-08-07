using Xunit;

namespace BSU.Core.Tests.Test
{
    /*
want:
storage mod lifecycle
matchmaker loading/hashing cycle, from dummy repo/storage
mathcmaker action, from dummy repo/storage
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
            var jobManager = new MockJobManager();
            var mod = new Model.StorageMod(null, implementation, "asdf", null, internalState, jobManager);
        }
    }
}