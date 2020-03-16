using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.Core.Services;
using BSU.Core.Sync;

namespace BSU.Core.Model
{
    internal class Model
    {
        // TODO: lock enumerables while stuff is being executed!!!
        internal MatchMaker MatchMaker { get; } = new MatchMaker();
        public List<Repository> Repositories { get; } = new List<Repository>();
        public List<Storage> Storages { get; } = new List<Storage>();

        private InternalState PersistentState { get; }

        public Model(InternalState persistentState)
        {
            PersistentState = persistentState;
        }

        public void Load()
        {
            foreach (var repository in PersistentState.LoadRepositories())
            {
                repository.Model = this;
                Repositories.Add(repository);
                RepositoryAdded?.Invoke(repository);
            }
            foreach (var storage in PersistentState.LoadStorages())
            {
                storage.Model = this;
                Storages.Add(storage);
                StorageAdded?.Invoke(storage);
            }
        }
        
        public event Action<Repository> RepositoryAdded;
        public event Action<Storage> StorageAdded;

        private void AddRepo()
        {
            
        }
        
        public void AddRepository(string type, string url, string name)
        {
            var repository = PersistentState.AddRepo(name, url, type, this);
            Repositories.Add(repository);
            RepositoryAdded?.Invoke(repository);
        }
        
        public void AddStorage(string type, DirectoryInfo dir, string name)
        {
            var storage = PersistentState.AddStorage(name, dir, type);
            Storages.Add(storage);
            StorageAdded?.Invoke(storage);
        }
    }
}