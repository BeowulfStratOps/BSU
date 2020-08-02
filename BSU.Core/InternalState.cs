using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.Core.JobManager;
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
        private readonly Types _types;

        private readonly List<Tuple<RepoEntry, Exception>> _repoErrors = new List<Tuple<RepoEntry, Exception>>();

        private readonly List<Tuple<StorageEntry, Exception>> _storageErrors =
            new List<Tuple<StorageEntry, Exception>>();

        // TODO: expose those to user
        public IReadOnlyList<Tuple<RepoEntry, Exception>> GetRepoErrors() => _repoErrors.AsReadOnly();
        public IReadOnlyList<Tuple<StorageEntry, Exception>> GetStorageErrors() => _storageErrors.AsReadOnly();

        internal InternalState(ISettings settings, Types types)
        {
            Logger.Info("Creating new internal state");
            _settings = settings;
            _types = types;
        }

        public List<Model.Storage> LoadStorages(IJobManager jobManager, MatchMaker matchMaker)
        {
            var result = new List<Model.Storage>();
            foreach (var storageEntry in _settings.Storages)
            {
                try
                {
                    result.Add(LoadStorage(storageEntry, jobManager, matchMaker));
                }
                catch (Exception e)
                {
                    _storageErrors.Add(Tuple.Create(storageEntry, e));
                }
            }

            return result;
        }

        public List<Repository> LoadRepositories(IJobManager jobManager, MatchMaker matchMaker)
        {
            var result = new List<Repository>();
            foreach (var repoEntry in _settings.Repositories)
            {
                try
                {
                    result.Add(LoadRepository(repoEntry, jobManager, matchMaker));
                }
                catch (Exception e)
                {
                    _repoErrors.Add(Tuple.Create(repoEntry, e));
                }
            }

            return result;
        }


        public void RemoveRepo(Repository repo)
        {
            Logger.Debug("Removing repo {0}", repo.Uid);
            var repoEntry = _settings.Repositories.Single(r => r.Name == repo.Identifier);
            _settings.Repositories.Remove(repoEntry);
            _settings.Store();
        }

        public Repository AddRepo(string name, string url, string type, Model.Model model, IJobManager jobManager, MatchMaker matchMaker)
        {
            if (_settings.Repositories.Any(r => r.Name == name)) throw new ArgumentException("Name in use");
            var repo = new RepoEntry
            {
                Name = name,
                Type = type,
                Url = url,
                UsedMods = new Dictionary<string, StorageModIdentifiers>()
            };
            _settings.Repositories.Add(repo);
            _settings.Store();
            return LoadRepository(repo, jobManager, matchMaker);
        }


        public Repository LoadRepository(RepoEntry repo, IJobManager jobManager, MatchMaker matchMaker)
        {
            Logger.Debug("Creating repo {0} / {1} / {2}", repo.Name, repo.Type, repo.Url);
            var implementation = _types.GetRepoImplementation(repo.Type, repo.Url);
            var repository = new Repository(implementation, repo.Name, repo.Url, jobManager, matchMaker, this);
            Logger.Debug("Created repo {0}", repository.Uid);
            return repository;
        }

        public Model.Storage AddStorage(string name, DirectoryInfo directory, string type, IJobManager jobManager, MatchMaker matchMaker)
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
            return LoadStorage(storage, jobManager, matchMaker);
        }

        public void RemoveStorage(Model.Storage storage)
        {
            Logger.Debug("Removing storage {0}", storage.Uid);
            var storageEntry = _settings.Storages.Single(s => s.Name == storage.Identifier);
            _settings.Storages.Remove(storageEntry);
            _settings.Store();
        }

        public Model.Storage LoadStorage(StorageEntry storage, IJobManager jobManager, MatchMaker matchMaker)
        {
            Logger.Debug("Adding storage {0} / {1} / {2}", storage.Name, storage.Type, storage.Path);
            var implementation = _types.GetStorageImplementation(storage.Type, storage.Path);
            var storageObj = new Model.Storage(implementation, storage.Name, storage.Path, this, jobManager, matchMaker);
            Logger.Debug("Created storage {0}", storageObj.Uid);
            return storageObj;
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
            if (!repository.UsedMods.TryGetValue(repositoryMod.Identifier, out var usedMod) || usedMod == null) return false;
            return usedMod.StorageIdentifier == storageMod.Storage.Identifier &&
                   usedMod.StorageIdentifier == storageMod.Identifier;
        }

        public bool HasUsedMod(RepositoryMod repositoryMod)
        {
            var repository = _settings.Repositories.Single(repo => repo.Name == repositoryMod.Repository.Identifier);
            return repository.UsedMods.ContainsKey(repositoryMod.Identifier);
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
