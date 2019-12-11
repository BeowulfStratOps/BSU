using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using BSU.Core.Sync;
using BSU.CoreInterface;

namespace BSU.Core.Tests
{
    internal class MockRepoSync : RepoSync
    {
        public MockRepoSync() : base(new MockRemoteMod(),
            new MockStorageMod())
        {
        }

        public new bool IsDone() => true;
    }
}