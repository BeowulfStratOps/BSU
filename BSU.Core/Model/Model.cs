using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.Core.JobManager;
using BSU.Core.Persistence;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal interface IActionQueue
    {
        void EnQueueAction(Action action);
    }
    internal class Model : IModel
    {
        private readonly IJobManager _jobManager;
        private readonly Types _types;
        private readonly IActionQueue _dispatcher;

        private readonly MatchMaker _matchMaker;
        public List<IModelRepository> Repositories { get; } = new List<IModelRepository>();
        public List<IModelStorage> Storages { get; } = new List<IModelStorage>();

        private InternalState PersistentState { get; }

        private RelatedActionsBag _relatedActionsBag;

        public Model(InternalState persistentState, IJobManager jobManager, Types types, IActionQueue dispatcher)
        {
            _matchMaker = new MatchMaker(this);
            _jobManager = jobManager;
            _types = types;
            _dispatcher = dispatcher;
            _relatedActionsBag = new RelatedActionsBag();
            PersistentState = persistentState;
        }

        public void Load()
        {
            foreach (var (repositoryEntry, repositoryState) in PersistentState.GetRepositories())
            {
                var implementation = _types.GetRepoImplementation(repositoryEntry.Type, repositoryEntry.Url);
                var repository = new Repository(implementation, repositoryEntry.Name, repositoryEntry.Url, _jobManager, _matchMaker, repositoryState, _dispatcher, _relatedActionsBag, this);
                Repositories.Add(repository);
                RepositoryAdded?.Invoke(repository);
            }
            foreach (var (storageEntry, storageState) in PersistentState.GetStorages())
            {
                IStorage implementation;
                try
                {
                    implementation = _types.GetStorageImplementation(storageEntry.Type, storageEntry.Path);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    continue;
                }
                var storage = new Storage(implementation, storageEntry.Name, storageEntry.Path, storageState, _jobManager, _matchMaker, _dispatcher);
                Storages.Add(storage);
                StorageAdded?.Invoke(storage);
            }
        }

        public event Action<Repository> RepositoryAdded;
        public event Action<IModelRepository> RepositoryDeleted;
        public event Action<Storage> StorageAdded;
        public event Action<IModelStorage> StorageDeleted;

        public void AddRepository(string type, string url, string name)
        {
            if (!_types.GetRepoTypes().Contains(type)) throw new ArgumentException();
            var repoState = PersistentState.AddRepo(name, url, type);
            var implementation = _types.GetRepoImplementation(type, url);
            var repository = new Repository(implementation, name, url, _jobManager, _matchMaker, repoState, _dispatcher, _relatedActionsBag, this);
            Repositories.Add(repository);
            RepositoryAdded?.Invoke(repository);
        }

        public void AddStorage(string type, DirectoryInfo dir, string name)
        {
            if (!_types.GetStorageTypes().Contains(type)) throw new ArgumentException();
            var storageState = PersistentState.AddStorage(name, dir, type);
            var implementation = _types.GetStorageImplementation(type, dir.FullName);
            var storage = new Storage(implementation, name, dir.FullName, storageState, _jobManager, _matchMaker, _dispatcher);
            Storages.Add(storage);
            StorageAdded?.Invoke(storage);
        }

        public IEnumerable<IModelStorage> GetStorages() => Storages;

        public IEnumerable<IModelRepository> GetRepositories() => Repositories;

        public IEnumerable<IModelStorageMod> GetAllStorageMods()
        {
            return Storages.SelectMany(storage => storage.Mods);
        }

        public IEnumerable<IModelRepositoryMod> GetAllRepositoryMods()
        {
            return Repositories.SelectMany(repository => repository.Mods);
        }

        public void DeleteRepository(IModelRepository repository, bool removeMods)
        {
            if (removeMods) throw new NotImplementedException();
            Repositories.Remove(repository);
            PersistentState.RemoveRepository(repository.Identifier);
            RepositoryDeleted?.Invoke(repository);
        }

        public void DeleteStorage(IModelStorage storage, bool removeMods)
        {
            if (removeMods) throw new NotImplementedException();
            Storages.Remove(storage);
            PersistentState.RemoveStorage(storage.Identifier);
            StorageDeleted?.Invoke(storage);
        }
    }
}
