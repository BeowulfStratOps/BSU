using System.Collections.Generic;
using BSU.Core.Model;

namespace BSU.Core.Tests.Mocks
{
    internal class MockModelStructure : IModelStructure
    {
        public IEnumerable<IModelStorage> GetStorages()
        {
            return new List<IModelStorage>();
        }

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