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
    internal class Model
    {
        private readonly IJobManager _jobManager;
        private readonly Types _types;

        private readonly MatchMaker _matchMaker;
        public List<Repository> Repositories { get; } = new List<Repository>();
        public List<Storage> Storages { get; } = new List<Storage>();

        private InternalState PersistentState { get; }

        private RelatedActionsBag _relatedActionsBag;
        
        private readonly Thread _spoolThread;
        private readonly ConcurrentQueue<Action> _spoolQueue = new ConcurrentQueue<Action>();
        private bool _running = true;

        public Model(InternalState persistentState, IJobManager jobManager, Types types)
        {
            _matchMaker = new MatchMaker(this);
            _jobManager = jobManager;
            _types = types;
            _relatedActionsBag = new RelatedActionsBag();
            PersistentState = persistentState;
            _spoolThread = new Thread(Spool);
        }

        public void Load()
        {
            foreach (var (repositoryEntry, repositoryState) in PersistentState.GetRepositories())
            {
                var implementation = _types.GetRepoImplementation(repositoryEntry.Type, repositoryEntry.Url);
                var repository = new Repository(implementation, repositoryEntry.Name, repositoryEntry.Url, _jobManager, _matchMaker, repositoryState, this, _relatedActionsBag);
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
            _spoolThread.Start();
        }

        public void Shutdown()
        {
            _running = false;
        }

        public void EnQueueAction(Action action)
        {
            _spoolQueue.Enqueue(action);
        }

        private void Spool()
        {
            while (_running)
            {
                if (!_spoolQueue.TryDequeue(out var action))
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
            var repository = new Repository(implementation, name, url, _jobManager, _matchMaker, repoState, this, _relatedActionsBag);
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
    }
}