using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BSU.Core.Model;

namespace BSU.Core.Tests.Mocks
{
    internal class MockModelStructure : IModelStructure
    {
        public List<IModelStorage> Storages { get; } = new();

        public IEnumerable<IModelStorage> GetStorages() => Storages;

        public IEnumerable<IModelRepository> GetRepositories()
        {
            return new List<Repository>();
        }

        public Task<IEnumerable<IModelStorageMod>> GetStorageMods()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IModelRepositoryMod>> GetRepositoryMods()
        {
            throw new NotImplementedException();
        }

        public List<IModelStorageMod> StorageMods { get; } = new();

        public List<IModelRepositoryMod> RepositoryMods { get; } = new();
    }
}
