using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.Core.Model;
using NLog;

namespace BSU.Core
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

        public IReadOnlyList<StorageEntry> GetStorages()
        {
            return _settings.Storages.AsReadOnly();
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

        public void AddStorage(string name, DirectoryInfo directory, string type)
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
        }

        public void RemoveStorage(Model.Storage storage)
        {
            Logger.Debug("Removing storage {0}", storage.Uid);
            var storageEntry = _settings.Storages.Single(s => s.Name == storage.Identifier);
            _settings.Storages.Remove(storageEntry);
            _settings.Store();
        }

        public void SetUpdatingTo(StorageMod mod, string targetHash, string targetDisplay)
        {
            Logger.Debug("Set updating: {0} to {1} : {2}", mod.Uid, targetHash, targetDisplay);
            var dic =_settings.Storages.Single(s => s.Name == mod.Storage.Identifier).Updating;
            dic[mod.Identifier] = new UpdateTarget(targetHash, targetDisplay);
            _settings.Store();
        }

        public void RemoveUpdatingTo(StorageMod mod)
        {
            Logger.Debug("Remove updating: {0}", mod.Uid);
            _settings.Storages.Single(s => s.Name == mod.Storage.Identifier).Updating
                .Remove(mod.Identifier);
            _settings.Store();
        }

        public void CleanupUpdatingTo(Model.Storage storage)
        {
            var updating = _settings.Storages.Single(s => s.Name == storage.Identifier).Updating;
            foreach (var modId in updating.Keys.ToList())
            {
                if (storage.Mods.Any(m => m.Identifier == modId)) continue;
                updating.Remove(modId);
                Logger.Debug("Cleaing up udpating for {0} / {1}", storage.Identifier, modId);
            }
        }

        public UpdateTarget GetUpdateTarget(StorageMod mod)
        {
            var target = _settings.Storages
                .SingleOrDefault(s => s.Name == mod.Storage.Identifier)?.Updating
                .GetValueOrDefault(mod.Identifier);
            return target == null ? null : new UpdateTarget(target.Hash, target.Display);
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
