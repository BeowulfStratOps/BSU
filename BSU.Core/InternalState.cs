using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.BSO;
using BSU.CoreInterface;

namespace BSU.Core
{
    class InternalState
    {
        private readonly Settings _settings;

        public InternalState(Settings settings)
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
            _settings.Repositories.Add(repo);
            _settings.Store();
            AddRepoToState(repo);
        }


        private void AddRepoToState(RepoEntry repo)
        {
            switch (repo.Type)
            {
                case "BSO":
                    var bsoRepo = new BsoRepo(repo.Url, repo.Name);
                    _repositories.Add(bsoRepo);
                    break;
                default:
                    throw new NotSupportedException($"Repo type {repo.Type} is not supported.");
            }
        }

        public void AddStorage(string name, DirectoryInfo directory, string type)
        {
            if (_settings.Storages.Any(s => s.Name == name)) throw new ArgumentException("Name in use");
            var storage = new StorageEntry
            {
                Name = name,
                Path = directory.FullName,
                Type = type,
                Updating = new Dictionary<string, string>()
            };
            _settings.Storages.Add(storage);
            _settings.Store();
            AddStorageToState(storage);
        }

        private void AddStorageToState(StorageEntry storage)
        {
            switch (storage.Type)
            {
                case "STEAM":
                    var steamStorage = new SteamStorage(storage.Path, storage.Name);
                    _storages.Add(steamStorage);
                    break;
                case "DIRECTORY":
                    var dirStorage = new DirectoryStorage(storage.Path, storage.Name);
                    _storages.Add(dirStorage);
                    break;
                default:
                    throw new NotSupportedException($"Storage type {storage.Type} is not supported.");
            }
        }

        public void PrintState()
        {
            Console.WriteLine("Repos:");
            foreach (var repository in _repositories)
            {
                Console.WriteLine($"  {repository.GetType().Name} {repository.GetName()} {repository.GetLocation()}");
            }
            Console.WriteLine("Storages:");
            foreach (var storage in _storages)
            {
                Console.WriteLine($"  {storage.GetType().Name} {storage.GetName()} {storage.GetLocation()}");
                foreach (var localMod in storage.GetMods())
                {
                    Console.WriteLine($"    {localMod.GetIdentifier()} | {localMod.GetDisplayName()} in {localMod.GetBaseDirectory().FullName}");
                }
            }
        }
    }
}
