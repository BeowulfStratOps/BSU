using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        public Task<IEnumerable<IModelStorageMod>> GetAllStorageMods() =>
            Task.FromResult((IEnumerable<IModelStorageMod>) StorageMods);

        public List<IModelRepositoryMod> RepositoryMods { get; } = new List<IModelRepositoryMod>();

        public Task<IEnumerable<IModelRepositoryMod>> GetAllRepositoryMods() =>
            Task.FromResult((IEnumerable<IModelRepositoryMod>) RepositoryMods);

        public event Action<IModelStorageMod> StorageModChanged;
    }
}
