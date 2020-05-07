using System;
using System.Collections.Generic;
using System.IO;
using BSU.Core.JobManager;

namespace BSU.Core.Model
{
    internal class Model
    {
        private readonly IJobManager _jobManager;

        // TODO: lock enumerables while stuff is being executed!!!
        internal MatchMaker MatchMaker { get; } = new MatchMaker();
        public List<Repository> Repositories { get; } = new List<Repository>();
        public List<Storage> Storages { get; } = new List<Storage>();

        private InternalState PersistentState { get; }

        public Model(InternalState persistentState, IJobManager jobManager)
        {
            _jobManager = jobManager;
            PersistentState = persistentState;
        }

        public void Load()
        {
            foreach (var repository in PersistentState.LoadRepositories(_jobManager))
            {
                repository.Model = this;
                Repositories.Add(repository);
                RepositoryAdded?.Invoke(repository);
            }
            foreach (var storage in PersistentState.LoadStorages(_jobManager))
            {
                storage.Model = this;
                Storages.Add(storage);
                StorageAdded?.Invoke(storage);
            }
        }
        
        public event Action<Repository> RepositoryAdded;
        public event Action<Storage> StorageAdded;
        
        public void AddRepository(string type, string url, string name)
        {
            var repository = PersistentState.AddRepo(name, url, type, this, _jobManager);
            Repositories.Add(repository);
            RepositoryAdded?.Invoke(repository);
        }
        
        public void AddStorage(string type, DirectoryInfo dir, string name)
        {
            var storage = PersistentState.AddStorage(name, dir, type, _jobManager);
            Storages.Add(storage);
            StorageAdded?.Invoke(storage);
        }
    }
}