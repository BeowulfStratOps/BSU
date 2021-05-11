using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.JobManager;
using BSU.Core.Persistence;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    public interface IActionQueue
    {
        void EnQueueAction(Action action);
    }
    internal class Model : IModel
    {
        private readonly IJobManager _jobManager;
        private readonly Types _types;
        private readonly IActionQueue _dispatcher;
        public List<IModelRepository> Repositories { get; } = new List<IModelRepository>();
        public List<IModelStorage> Storages { get; } = new List<IModelStorage>();

        private InternalState PersistentState { get; }

        private readonly RelatedActionsBag _relatedActionsBag;

        private readonly MatchMaker _matchMaker = new();

        public Model(InternalState persistentState, IJobManager jobManager, Types types, IActionQueue dispatcher)
        {
            _jobManager = jobManager;
            _types = types;
            _dispatcher = dispatcher;
            _relatedActionsBag = new RelatedActionsBag();
            PersistentState = persistentState;
        }

        public async Task Load()
        {
            foreach (var (repositoryEntry, repositoryState) in PersistentState.GetRepositories())
            {
                var implementation = _types.GetRepoImplementation(repositoryEntry.Type, repositoryEntry.Url);
                var repository = new Repository(implementation, repositoryEntry.Name, repositoryEntry.Url, _jobManager, repositoryState, _dispatcher, _relatedActionsBag, this);
                repository.ModAdded += mod => _matchMaker.AddRepoMod(mod);
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
                var storage = new Storage(implementation, storageEntry.Name, storageEntry.Path, storageState, _jobManager, _dispatcher);
                storage.ModAdded += mod => _matchMaker.AddStorageMod(mod);
                Storages.Add(storage);
                StorageAdded?.Invoke(storage);
            }

            await Task.WhenAll(Storages.Select(s => s.Load()));
            _matchMaker.SignalAllStorageModsLoaded(); // TODO: make sure this actually happens AFTER the previous line??
            await Task.WhenAll(Repositories.Select(r => r.Load()));
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
            var repository = new Repository(implementation, name, url, _jobManager, repoState, _dispatcher, _relatedActionsBag, this);
            repository.ModAdded += mod => _matchMaker.AddRepoMod(mod);
            Repositories.Add(repository);
            RepositoryAdded?.Invoke(repository);
        }

        public void AddStorage(string type, DirectoryInfo dir, string name)
        {
            if (!_types.GetStorageTypes().Contains(type)) throw new ArgumentException();
            var storageState = PersistentState.AddStorage(name, dir, type);
            var implementation = _types.GetStorageImplementation(type, dir.FullName);
            var storage = new Storage(implementation, name, dir.FullName, storageState, _jobManager, _dispatcher);
            storage.ModAdded += mod => _matchMaker.AddStorageMod(mod);
            Storages.Add(storage);
            StorageAdded?.Invoke(storage);
        }

        public IEnumerable<IModelStorage> GetStorages() => Storages;

        public IEnumerable<IModelRepository> GetRepositories() => Repositories;

        public async Task<IEnumerable<IModelStorageMod>> GetAllStorageMods()
        {
            var mods = new List<IModelStorageMod>();
            foreach (var storage in Storages)
            {
                mods.AddRange(await storage.GetMods());
            }
            return mods;
        }

        public async Task<IEnumerable<IModelRepositoryMod>> GetAllRepositoryMods()
        {
            var mods = new List<IModelRepositoryMod>();
            foreach (var repository in Repositories)
            {
                mods.AddRange(await repository.GetMods());
            }
            return mods;
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
