using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.Core.Model;
using NLog;

namespace BSU.Core.Persistence
{
    /// <summary>
    /// Internal state of the core. Knows locations, but no repo/storage states.
    /// Tracks the state across restarts by using a settings file.
    /// </summary>
    internal class InternalState : IInternalState
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly ISettings _settings;

        internal InternalState(ISettings settings)
        {
            Logger.Info("Creating new internal state");
            _settings = settings;
        }

        public IEnumerable<Tuple<IStorageEntry, IStorageState>> GetStorages()
        {
            return _settings.Storages.Select(entry =>
                new Tuple<IStorageEntry, IStorageState>(entry, new StorageState(entry, _settings.Store)));
        }

        public IEnumerable<Tuple<IRepositoryEntry, IRepositoryState>> GetRepositories()
        {
            return _settings.Repositories.Select(entry =>
                new Tuple<IRepositoryEntry, IRepositoryState>(entry, new RepositoryState(entry, _settings.Store)));
        }
        
        public void RemoveRepository(string repositoryIdentifier)
        {
            Logger.Debug("Removing repo {0}", repositoryIdentifier);
            var repoEntry = _settings.Repositories.Single(r => r.Name == repositoryIdentifier);
            _settings.Repositories.Remove(repoEntry);
            _settings.Store();
        }

        public IRepositoryState AddRepo(string name, string url, string type)
        {
            if (_settings.Repositories.Any(r => r.Name == name)) throw new ArgumentException("Name in use");
            var repo = new RepositoryEntry(name, type, url);
            _settings.Repositories.Add(repo);
            _settings.Store();
            return new RepositoryState(repo, _settings.Store);
        }

        public IStorageState AddStorage(string name, DirectoryInfo directory, string type)
        {
            if (_settings.Storages.Any(s => s.Name == name)) throw new ArgumentException("Name in use");
            var storage = new StorageEntry(name, directory.FullName, type);
            _settings.Storages.Add(storage);
            _settings.Store();
            return new StorageState(storage, _settings.Store);
        }
        
        public void RemoveStorage(string storageIdentifier)
        {
            Logger.Debug("Removing storage {0}", storageIdentifier);
            var storageEntry = _settings.Storages.Single(s => s.Name == storageIdentifier);
            _settings.Storages.Remove(storageEntry);
            _settings.Store();
        }
    }
}
