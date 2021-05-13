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

        public List<IModelStorageMod> StorageMods { get; } = new();

        public IEnumerable<IModelStorageMod> GetAllStorageMods() => StorageMods;

        public List<IModelRepositoryMod> RepositoryMods { get; } = new();

        public IEnumerable<IModelRepositoryMod> GetAllRepositoryMods() => RepositoryMods;
    }
}
