﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.BSO;
using BSU.Core.Storage;
using BSU.CoreCommon;
using NLog;
using NLog.Fluent;

namespace BSU.Core
{
    class InternalState
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, Func<string, string, IRepository>> _repoTypes =
            new Dictionary<string, Func<string, string, IRepository>>
            {
                {"BSO", (name, url) => new BsoRepo(url, name)}
            };

        private readonly Dictionary<string, Func<string, string, IStorage>> _storageTypes =
            new Dictionary<string, Func<string, string, IStorage>>
            {
                {"STEAM", (name, path) => new SteamStorage(path, name)},
                {"DIRECTORY", (name, path) => new DirectoryStorage(path, name)}
            };

        private readonly ISettings _settings;

        internal void AddRepoType(string name, Func<string, string, IRepository> create) => _repoTypes.Add(name, create);
        internal IEnumerable<string> GetRepoTypes() => _repoTypes.Keys.ToList();
        internal void AddStorageType(string name, Func<string, string, IStorage> create) => _storageTypes.Add(name, create);
        internal IEnumerable<string> GetStorageTypes() => _storageTypes.Keys.ToList();

        private readonly List<Tuple<RepoEntry, Exception>> _repoErrors = new List<Tuple<RepoEntry, Exception>>();
        private readonly List<Tuple<StorageEntry, Exception>> _storageErrors = new List<Tuple<StorageEntry, Exception>>();
        // TODO: expose those to user
        public IReadOnlyList<Tuple<RepoEntry, Exception>> GetRepoErrors() => _repoErrors.AsReadOnly();
        public IReadOnlyList<Tuple<StorageEntry, Exception>> GetStorageErrors() => _storageErrors.AsReadOnly();

        public InternalState(ISettings settings)
        {
            Logger.Info("Creating new internal state");
            _settings = settings;
            foreach (var repoEntry in _settings.Repositories)
            {
                try
                {
                    AddRepoToState(repoEntry);
                }
                catch (Exception e)
                {
                    _repoErrors.Add(Tuple.Create(repoEntry, e));
                }
            }
            foreach (var storageEntry in _settings.Storages)
            {
                try
                {
                    AddStorageToState(storageEntry);
                }
                catch (Exception e)
                {
                    _storageErrors.Add(Tuple.Create(storageEntry, e));
                }
            }
        }

        private readonly List<IRepository> _repositories = new List<IRepository>();
        private readonly List<IStorage> _storages = new List<IStorage>();

        public IReadOnlyList<IRepository> GetRepositories() => _repositories.AsReadOnly();
        public IReadOnlyList<IStorage> GetStorages() => _storages.AsReadOnly();


        public void RemoveRepo(IRepository repo)
        {
            Logger.Debug("Removing repo {0}", repo.GetUid());
            var repoEntry = _settings.Repositories.Single(r => r.Name == repo.GetIdentifier());
            _repositories.Remove(repo);
            _settings.Repositories.Remove(repoEntry);
            _settings.Store();
        }

        public void AddRepo(string name, string url, string type)
        {
            if (_settings.Repositories.Any(r => r.Name == name)) throw new ArgumentException("Name in use");
            var repo = new RepoEntry
            {
                Name = name,
                Type = type,
                Url = url
            };
            AddRepoToState(repo);
            _settings.Repositories.Add(repo);
            _settings.Store();
        }


        private void AddRepoToState(RepoEntry repo)
        {
            if (!_repoTypes.TryGetValue(repo.Type, out var create)) throw new NotSupportedException($"Repo type {repo.Type} is not supported.");

            Logger.Debug("Adding repo {0} / {1} / {2}", repo.Name, repo.Type, repo.Url);
            var repository = create(repo.Name, repo.Url);
            Logger.Debug("Created repo {0}", repository.GetUid());
            _repositories.Add(repository);
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
            AddStorageToState(storage);
            _settings.Storages.Add(storage);
            _settings.Store();
        }

        public void RemoveStorage(IStorage storage)
        {
            Logger.Debug("Removing storage {0}", storage.GetUid());
            var storageEntry = _settings.Storages.Single(s => s.Name == storage.GetIdentifier());
            _storages.Remove(storage);
            _settings.Storages.Remove(storageEntry);
            _settings.Store();
        }

        private void AddStorageToState(StorageEntry storage)
        {
            if (!_storageTypes.TryGetValue(storage.Type, out var create)) throw new NotSupportedException($"Storage type {storage.Type} is not supported.");

            Logger.Debug("Adding storage {0} / {1} / {2}", storage.Name, storage.Type, storage.Path);
            var storageObj = create(storage.Name, storage.Path);
            Logger.Debug("Created storage {0}", storageObj.GetUid());
            _storages.Add(storageObj);
        }

        public void PrintState()
        {
            Console.WriteLine("Repos:");
            foreach (var repository in _repositories)
            {
                Console.WriteLine($"  {repository.GetType().Name} {repository.GetIdentifier()} {repository.GetLocation()}");
                foreach (var repoMod in repository.GetMods())
                {
                    Console.WriteLine($"    {repoMod.GetIdentifier()} | {repoMod.GetDisplayName()}");
                }
            }
            Console.WriteLine("Storages:");
            foreach (var storage in _storages)
            {
                Console.WriteLine($"  {storage.GetType().Name} {storage.GetIdentifier()} {storage.GetLocation()}");
                foreach (var storageMod in storage.GetMods())
                {
                    Console.WriteLine($"    {storageMod.GetIdentifier()} | {storageMod.GetDisplayName()} in {storage.GetIdentifier()}");
                }
            }
        }

        public void SetUpdatingTo(IStorageMod mod, string targetHash, string targetDisplay)
        {
            Logger.Debug("Set updating: {0} to {1} : {2}", mod.GetUid(), targetHash, targetDisplay);
            _settings.Storages.Single(s => s.Name == mod.GetStorage().GetIdentifier()).Updating[mod.GetIdentifier()] = new UpdateTarget(targetHash, targetDisplay);
            _settings.Store();
        }

        public void RemoveUpdatingTo(IStorageMod mod)
        {
            Logger.Debug("Remove updating: {0}", mod.GetUid());
            _settings.Storages.Single(s => s.Name == mod.GetStorage().GetIdentifier()).Updating.Remove(mod.GetIdentifier());
            _settings.Store();
        }

        public void CleanupUpdatingTo(IStorage storage)
        {
            var updating = _settings.Storages.Single(s => s.Name == storage.GetIdentifier()).Updating;
            foreach (var modId in updating.Keys.ToList())
            {
                if (storage.GetMods().All(m => m.GetIdentifier() != modId))
                {
                    updating.Remove(modId);
                    Logger.Debug("Cleaing up udpating for {0} / {1}", storage.GetIdentifier(), modId);
                }
            }
        }

        public UpdateTarget GetUpdateTarget(IStorageMod mod)
        {
            var target = _settings.Storages
                .SingleOrDefault(s => s.Name == mod.GetStorage().GetIdentifier())?.Updating.GetValueOrDefault(mod.GetIdentifier());
            if (target == null) return null;
            return new UpdateTarget(target.Hash, target.Display);
        }
    }
}
