using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.Core.Persistence;

namespace BSU.Core.Model
{
    internal class Model : IModel
    {
        private readonly Types _types;

        private readonly ModelStructure _structure = new();

        private InternalState PersistentState { get; }

        public Model(InternalState persistentState, Types types)
        {
            _types = types;
            PersistentState = persistentState;
        }

        public void Load()
        {
            foreach (var (repositoryEntry, repositoryState) in PersistentState.GetRepositories())
            {
                try
                {
                    var repository = CreateRepository(repositoryEntry, repositoryState);
                    _structure.AddRepository(repository);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            foreach (var (storageEntry, storageState) in PersistentState.GetStorages())
            {
                try
                {
                    var storage = CreateStorage(storageEntry, storageState);
                    _structure.AddStorage(storage);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private Repository CreateRepository(IRepositoryEntry data, IRepositoryState state)
        {
            var implementation = _types.GetRepoImplementation(data.Type, data.Url);
            var repository = new Repository(implementation, data.Name, data.Url, state, _structure);
            // TODO: kick off mods
            return repository;
        }

        private Storage CreateStorage(IStorageEntry data, IStorageState state)
        {
            var implementation = _types.GetStorageImplementation(data.Type, data.Path);
            var storage = new Storage(implementation, data.Name, data.Path, state);
            // TODO: kick off mods
            return storage;
        }

        public IModelRepository AddRepository(string type, string url, string name)
        {
            if (!_types.GetRepoTypes().Contains(type)) throw new ArgumentException();
            var (entry, repoState) = PersistentState.AddRepo(name, url, type);
            return CreateRepository(entry, repoState);
        }

        public IModelStorage AddStorage(string type, DirectoryInfo dir, string name)
        {
            if (!_types.GetStorageTypes().Contains(type)) throw new ArgumentException();
            var (entry, storageState) = PersistentState.AddStorage(name, dir, type);
            return CreateStorage(entry, storageState);
        }

        public IEnumerable<IModelStorage> GetStorages() => _structure.GetStorages();

        public IEnumerable<IModelRepository> GetRepositories() => _structure.GetRepositories();
        public void DeleteRepository(IModelRepository repository, bool removeMods)
        {
            if (removeMods) throw new NotImplementedException();
            _structure.RemoveRepository(repository);
            PersistentState.RemoveRepository(repository.Identifier);
            // TODO: dispose / stop actions
        }

        public void DeleteStorage(IModelStorage storage, bool removeMods)
        {
            if (removeMods) throw new NotImplementedException();
            _structure.RemoveStorage(storage);
            PersistentState.RemoveStorage(storage.Identifier);
            // TODO: dispose / stop actions
        }
    }
}
