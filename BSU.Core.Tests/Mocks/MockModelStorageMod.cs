using System;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.Core.Persistence;
using BSU.CoreCommon;

namespace BSU.Core.Tests.Mocks
{
    internal class MockModelStorageMod : IModelStorageMod
    {
        public event Action StateChanged;

        public IUpdateState PrepareUpdate(IRepositoryMod repositoryMod, UpdateTarget target, MatchHash targetMatch, VersionHash targetVersion)
        {
            throw new NotImplementedException();
        }

        public Task<VersionHash> GetVersionHash()
        {
            throw new NotImplementedException();
        }

        public Task<MatchHash> GetMatchHash()
        {
            throw new NotImplementedException();
        }

        public StorageModStateEnum GetState()
        {
            throw new NotImplementedException();
        }

        public void Abort()
        {
            throw new NotImplementedException();
        }

        public PersistedSelection GetStorageModIdentifiers()
        {
            throw new NotImplementedException();
        }

        public bool CanWrite { get; }
        public string Identifier { get; }
        public IStorageMod Implementation { get; }
    }
}
