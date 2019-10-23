﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.BSO;
using BSU.Core.State;
using BSU.CoreInterface;

namespace BSU.Core
{
    class InternalState
    {
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
        internal void AddStorageType(string name, Func<string, string, IStorage> create) => _storageTypes.Add(name, create);

        public InternalState(ISettings settings)
        {
            _settings = settings;
            foreach (var repoEntry in _settings.Repositories)
            {
                AddRepoToState(repoEntry);
            }
            foreach (var storageEntry in _settings.Storages)
            {
                AddStorageToState(storageEntry);
            }
        }

        private readonly List<IRepository> _repositories = new List<IRepository>();
        private readonly List<IStorage> _storages = new List<IStorage>();

        public IReadOnlyList<IRepository> GetRepositories() => _repositories.AsReadOnly();
        public IReadOnlyList<IStorage> GetStorages() => _storages.AsReadOnly();


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

            var repository = create(repo.Name, repo.Url);
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

        private void AddStorageToState(StorageEntry storage)
        {
            if (!_storageTypes.TryGetValue(storage.Type, out var create)) throw new NotSupportedException($"Storage type {storage.Type} is not supported.");

            var storageObj = create(storage.Name, storage.Path);
            _storages.Add(storageObj);
        }

        public void PrintState()
        {
            Console.WriteLine("Repos:");
            foreach (var repository in _repositories)
            {
                Console.WriteLine($"  {repository.GetType().Name} {repository.GetName()} {repository.GetLocation()}");
                foreach (var localMod in repository.GetMods())
                {
                    Console.WriteLine($"    {localMod.GetIdentifier()} | {localMod.GetDisplayName()}");
                }
            }
            Console.WriteLine("Storages:");
            foreach (var storage in _storages)
            {
                Console.WriteLine($"  {storage.GetType().Name} {storage.GetName()} {storage.GetLocation()}");
                foreach (var localMod in storage.GetMods())
                {
                    Console.WriteLine($"    {localMod.GetIdentifier()} | {localMod.GetDisplayName()} in {storage.GetName()}");
                }
            }
        }

        public void SetUpdatingTo(StorageMod mod, string targetHash, string targetDisplay)
        {
            _settings.Storages.Single(s => s.Name == mod.Storage.Name).Updating[mod.Name] = new UpdateTarget(targetHash, targetDisplay);
            _settings.Store();
        }

        public void RemoveUpdatingTo(StorageMod mod)
        {
            _settings.Storages.Single(s => s.Name == mod.Storage.Name).Updating.Remove(mod.Name);
            _settings.Store();
        }

        public UpdateTarget GetUpdateTarget(StorageMod mod)
        {
            var target = _settings.Storages
                .SingleOrDefault(s => s.Name == mod.Storage.Name)?.Updating.GetValueOrDefault(mod.Name);
            if (target == null) return null;
            return new UpdateTarget(target.Hash, target.Display);
        }
    }
}
