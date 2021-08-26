using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BSU.Core.Model
{
    internal class ModelStructure : IModelStructure
    {
        // TODO: create cts to revoke operations and lock. lock(set cts, new cts, do changes) for changes

        private readonly List<IModelRepository> _repositories = new();
        private readonly List<IModelStorage> _storages = new();

        public IEnumerable<IModelStorage> GetStorages() => _storages;

        public IEnumerable<IModelRepository> GetRepositories() => _repositories;

        public void AddRepository(IModelRepository repository) => _repositories.Add(repository);
        public void AddStorage(IModelStorage storage) => _storages.Add(storage);

        public void RemoveRepository(IModelRepository repository) => _repositories.Remove(repository);
        public void RemoveStorage(IModelStorage storage) => _storages.Remove(storage);

        // TODO: make async enumerable
        public async Task<IEnumerable<IModelStorageMod>> GetStorageMods()
        {
            var modTasks = _storages.Select(s => s.GetMods()).ToList();
            await Task.WhenAll(modTasks);
            return modTasks.SelectMany(t => t.Result);
        }

        // TODO: make async enumerable
        public async Task<IEnumerable<IModelRepositoryMod>> GetRepositoryMods()
        {
            var modTasks = _repositories.Select(s => s.GetMods()).ToList();
            await Task.WhenAll(modTasks);
            return modTasks.SelectMany(t => t.Result);
        }
    }
}
