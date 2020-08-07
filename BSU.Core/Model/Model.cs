using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BSU.Core.JobManager;
using BSU.Core.Persistence;

namespace BSU.Core.Model
{
    internal interface IActionQueue
    {
        void EnQueueAction(Action action);
    }
    internal class Model : IActionQueue, IModelStructure
    {
        private readonly IJobManager _jobManager;
        private readonly Types _types;

        private readonly MatchMaker _matchMaker;
        public List<IModelRepository> Repositories { get; } = new List<IModelRepository>();
        public List<IModelStorage> Storages { get; } = new List<IModelStorage>();

        private InternalState PersistentState { get; }

        private RelatedActionsBag _relatedActionsBag;
        
        private readonly Thread _actionsQueueThread;
        private readonly ConcurrentQueue<Action> _actionsQueue = new ConcurrentQueue<Action>();
        private bool _running = true;

        public Model(InternalState persistentState, IJobManager jobManager, Types types)
        {
            _matchMaker = new MatchMaker(this);
            _jobManager = jobManager;
            _types = types;
            _relatedActionsBag = new RelatedActionsBag();
            PersistentState = persistentState;
            _actionsQueueThread = new Thread(DoQueuedActions);
        }

        public void Load()
        {
            foreach (var (repositoryEntry, repositoryState) in PersistentState.GetRepositories())
            {
                var implementation = _types.GetRepoImplementation(repositoryEntry.Type, repositoryEntry.Url);
                var repository = new Repository(implementation, repositoryEntry.Name, repositoryEntry.Url, _jobManager, _matchMaker, repositoryState, this, _relatedActionsBag, this);
                Repositories.Add(repository);
                RepositoryAdded?.Invoke(repository);
            }
            foreach (var (storageEntry, storageState) in PersistentState.GetStorages())
            {
                var implementation = _types.GetStorageImplementation(storageEntry.Type, storageEntry.Path);
                var storage = new Storage(implementation, storageEntry.Name, storageEntry.Path, storageState, _jobManager, _matchMaker, this);
                Storages.Add(storage);
                StorageAdded?.Invoke(storage);
            }
            _actionsQueueThread.Start();
        }

        public void Shutdown()
        {
            _running = false;
        }

        public void EnQueueAction(Action action)
        {
            _actionsQueue.Enqueue(action);
        }

        private void DoQueuedActions()
        {
            while (_running)
            {
                if (!_actionsQueue.TryDequeue(out var action))
                {
                    // TODO: use event / some sort of signaling
                    Thread.Sleep(50);
                    continue;
                }
                action();
            }
        }
        
        public event Action<Repository> RepositoryAdded;
        public event Action<Storage> StorageAdded;
        
        public void AddRepository(string type, string url, string name)
        {
            if (!_types.GetRepoTypes().Contains(type)) throw new ArgumentException();
            var repoState = PersistentState.AddRepo(name, url, type);
            var implementation = _types.GetRepoImplementation(type, url);
            var repository = new Repository(implementation, name, url, _jobManager, _matchMaker, repoState, this, _relatedActionsBag, this);
            Repositories.Add(repository);
            RepositoryAdded?.Invoke(repository);
        }
        
        public void AddStorage(string type, DirectoryInfo dir, string name)
        {
            if (!_types.GetStorageTypes().Contains(type)) throw new ArgumentException();
            var storageState = PersistentState.AddStorage(name, dir, type);
            var implementation = _types.GetStorageImplementation(type, dir.FullName);
            var storage = new Storage(implementation, name, dir.FullName, storageState, _jobManager, _matchMaker, this);
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
    }
}