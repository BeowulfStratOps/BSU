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

        public IReadOnlyList<Tuple<StorageEntry, IStorageState>> GetStorages()
        {
            return _settings.Storages
                .Select(entry => Tuple.Create(entry, (IStorageState) new StorageState(entry, _settings.Store)))
                .ToList();
        }

        public IReadOnlyList<RepositoryEntry> GetRepositories()
        {
            return _settings.Repositories.AsReadOnly();
        }
        
        public void RemoveRepo(Repository repo)
        {
            Logger.Debug("Removing repo {0}", repo.Uid);
            var repoEntry = _settings.Repositories.Single(r => r.Name == repo.Identifier);
            _settings.Repositories.Remove(repoEntry);
            _settings.Store();
        }

        public void AddRepo(string name, string url, string type)
        {
            if (_settings.Repositories.Any(r => r.Name == name)) throw new ArgumentException("Name in use");
            var repo = new RepositoryEntry
            {
                Name = name,
                Type = type,
                Url = url,
                UsedMods = new Dictionary<string, StorageModIdentifiers>()
            };
            _settings.Repositories.Add(repo);
            _settings.Store();
        }

        public IStorageState AddStorage(string name, DirectoryInfo directory, string type)
        {
            if (_settings.Storages.Any(s => s.Name == name)) throw new ArgumentException("Name in use");
            var storage = new StorageEntry
            {
                Name = name,
                Path = directory.FullName,
                Type = type,
                Updating = new Dictionary<string, UpdateTarget>()
            };
            _settings.Storages.Add(storage);
            _settings.Store();
            return new StorageState(storage, _settings.Store);
        }
        
        public void RemoveStorage(StorageEntry storage)
        {
            Logger.Debug("Removing storage {0}", storage.Name);
            var storageEntry = _settings.Storages.Single(s => s.Name == storage.Name);
            _settings.Storages.Remove(storageEntry);
            _settings.Store();
        }

        public bool IsUsedMod(RepositoryMod repositoryMod, StorageMod storageMod)
        {
            var repository = _settings.Repositories.Single(repo => repo.Name == repositoryMod.Repository.Identifier);
            if (repository.UsedMods == null) return false;
            if (!repository.UsedMods.TryGetValue(repositoryMod.Identifier, out var usedMod) || usedMod == null) return false;
            return usedMod.StorageIdentifier == storageMod.Storage.Identifier &&
                   usedMod.StorageIdentifier == storageMod.Identifier;
        }

        public bool HasUsedMod(RepositoryMod repositoryMod)
        {
            var repository = _settings.Repositories.Single(repo => repo.Name == repositoryMod.Repository.Identifier);
            return repository.UsedMods?.ContainsKey(repositoryMod.Identifier) ?? false;
        }

        public void SetUsedMod(RepositoryMod repositoryMod, StorageMod storageMod)
        {
            var repository = _settings.Repositories.Single(repo => repo.Name == repositoryMod.Repository.Identifier);
            repository.UsedMods[repositoryMod.Identifier] = new StorageModIdentifiers
            {
                StorageIdentifier = storageMod.Storage.Identifier,
                ModIdentifier = storageMod.Identifier
            };
            _settings.Store();
        }
    }
}
