using System.Collections.Generic;
using BSU.Core.Model;

namespace BSU.Core.Tests.Mocks
{
    internal class MockModelStructure : IModelStructure
    {
        public List<IModelStorage> Storages { get; } = new List<IModelStorage>();

        public IEnumerable<IModelStorage> GetStorages() => Storages;

        public IEnumerable<IModelRepository> GetRepositories()
        {
            return new List<Repository>();
        }
        
        public List<IModelStorageMod> StorageMods { get; } = new List<IModelStorageMod>();

        public IEnumerable<IModelStorageMod> GetAllStorageMods() => StorageMods;
        
        public List<IModelRepositoryMod> RepositoryMods { get; } = new List<IModelRepositoryMod>();

        public IEnumerable<IModelRepositoryMod> GetAllRepositoryMods() => RepositoryMods;
    }
}