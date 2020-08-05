using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BSU.Core.JobManager;

namespace BSU.Core.Model
{
    internal class Model
    {
        private readonly IJobManager _jobManager;

        // TODO: lock enumerables while stuff is being executed!!!
        private readonly MatchMaker _matchMaker = new MatchMaker();
        public List<Repository> Repositories { get; } = new List<Repository>();
        public List<Storage> Storages { get; } = new List<Storage>();

        private InternalState PersistentState { get; }

        private readonly Thread _spoolThread;
        private readonly ConcurrentQueue<Action> _spoolQueue = new ConcurrentQueue<Action>();
        private bool _running = true;

        public Model(InternalState persistentState, IJobManager jobManager)
        {
            _jobManager = jobManager;
            PersistentState = persistentState;
            _spoolThread = new Thread(Spool);
        }

        public void Load()
        {
            foreach (var repository in PersistentState.LoadRepositories(_jobManager, _matchMaker, this))
            {
                Repositories.Add(repository);
                RepositoryAdded?.Invoke(repository);
            }
            foreach (var storage in PersistentState.LoadStorages(_jobManager, _matchMaker, this))
            {
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
            var repository = PersistentState.AddRepo(name, url, type, this, _jobManager, _matchMaker);
            Repositories.Add(repository);
            RepositoryAdded?.Invoke(repository);
        }
        
        public void AddStorage(string type, DirectoryInfo dir, string name)
        {
            var storage = PersistentState.AddStorage(name, dir, type, _jobManager, _matchMaker, this);
            Storages.Add(storage);
            StorageAdded?.Invoke(storage);
        }
    }
}