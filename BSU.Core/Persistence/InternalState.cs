using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.Launch;
using NLog;

namespace BSU.Core.Persistence
{
    /// <summary>
    /// Internal state of the core. Knows locations, but no repo/storage states.
    /// Tracks the state across restarts by using a settings file.
    /// </summary>
    internal class InternalState : IInternalState
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly ISettings _settings;

        internal InternalState(ISettings settings)
        {
            _logger.Info("Creating new internal state");
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

        public void RemoveRepository(Guid repositoryIdentifier)
        {
            _logger.Debug($"Removing repo {repositoryIdentifier}");
            var repoEntry = _settings.Repositories.Single(r => r.Guid == repositoryIdentifier);
            _settings.Repositories.Remove(repoEntry);
            _settings.Store();
        }

        public (IRepositoryEntry entry, IRepositoryState state) AddRepo(string name, string url, string type)
        {
            if (_settings.Repositories.Any(r => r.Name == name)) throw new ArgumentException("Name in use");
            var repo = new RepositoryEntry(name, type, url, Guid.NewGuid());
            _settings.Repositories.Add(repo);
            _settings.Store();
            return (repo, new RepositoryState(repo, _settings.Store));
        }

        public (IStorageEntry entry, IStorageState state) AddStorage(string name, string path, string type)
        {
            if (_settings.Storages.Any(s => s.Name == name)) throw new ArgumentException("Name in use");
            var storage = new StorageEntry(name, type, path, Guid.NewGuid());
            _settings.Storages.Add(storage);
            _settings.Store();
            return (storage, new StorageState(storage, _settings.Store));
        }

        public GlobalSettings Settings
        {
            get => _settings.GlobalSettings;
            set
            {
                _settings.GlobalSettings = value;
                _settings.Store();
            }
        }

        public void RemoveStorage(Guid storageIdentifier)
        {
            _logger.Debug($"Removing storage {storageIdentifier}");
            var storageEntry = _settings.Storages.Single(s => s.Guid == storageIdentifier);
            _settings.Storages.Remove(storageEntry);
            _settings.Store();
        }

        public bool CheckIsFirstStart()
        {
            if (_settings.FirstStartDone) return false;
            _logger.Debug("First startup");
            _settings.FirstStartDone = true;
            _settings.Store();
            return true;
        }
    }
}
