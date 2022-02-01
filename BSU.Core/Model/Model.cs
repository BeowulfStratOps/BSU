﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Ioc;
using BSU.Core.Launch;
using BSU.Core.Persistence;
using BSU.Core.Services;
using BSU.Core.Storage;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class Model : IModel
    {
        private readonly List<IModelRepository> _repositories = new();
        private readonly List<IModelStorage> _storages = new();

        private readonly ErrorPresenter _errorPresenter = new();

        public event Action<IModelRepository>? AddedRepository;
        public event Action<IModelStorage>? AddedStorage;
        public List<IModelStorageMod> GetStorageMods()
        {
            return _storages.Where(r => r.State == LoadingState.Loaded)
                .SelectMany(r => r.GetMods()).ToList();
        }

        public List<IModelRepositoryMod> GetRepositoryMods()
        {
            return _repositories.Where(r => r.State == LoadingState.Loaded)
                .SelectMany(r => r.GetMods()).ToList();
        }

        public event Action<IModelRepository>? RemovedRepository;
        public event Action<IModelStorage>? RemovedStorage;
        public event Action? AnyChange;

        private IInternalState PersistentState { get; }
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly IServiceProvider _services;

        public Model(IInternalState persistentState, Types types, IDispatcher dispatcher, bool isFirstStart)
        {
            PersistentState = persistentState;
            if (isFirstStart)
            {
                DoFirstStartSetup();
            }

            var services = new ServiceProvider();
            _services = services;
            services.Add(types);
            services.Add(dispatcher);
            services.Add<IErrorPresenter>(_errorPresenter);

            // TODO: they aren't really services in that sense... figure out if there's a better way
            services.Add(new AutoSelector(this));
            services.Add(new PresetGenerator(this));


            var eventCombiner = new StructureEventCombiner(this);

            eventCombiner.AnyChange += () => AnyChange?.Invoke();
            Load();
        }

        private void DoFirstStartSetup()
        {
            _logger.Info("First start setup");
            var steamPath = SteamStorage.GetWorkshopPath();
            if (steamPath == null)
            {
                _logger.Info("No steam workshop path found. not adding steam storage");
                return;
            }
            _logger.Info($"Found steam at {steamPath}. Adding steam storage");
            PersistentState.AddStorage("Steam", steamPath, "STEAM");
        }

        private void Load()
        {
            foreach (var (repositoryEntry, repositoryState) in PersistentState.GetRepositories())
            {
                CreateRepository(repositoryEntry, repositoryState);
            }
            foreach (var (storageEntry, storageState) in PersistentState.GetStorages())
            {
                CreateStorage(storageEntry, storageState);
            }
        }

        private Repository CreateRepository(IRepositoryEntry data, IRepositoryState state)
        {
            var types = _services.Get<Types>();
            var implementation = types.GetRepoImplementation(data.Type, data.Url);
            var repository = new Repository(implementation, data.Name, data.Url, state, _services);
            _repositories.Add(repository);
            AddedRepository?.Invoke(repository);
            return repository;
        }

        private Storage CreateStorage(IStorageEntry data, IStorageState state)
        {
            var types = _services.Get<Types>();
            var implementation = types.GetStorageImplementation(data.Type, data.Path);
            var storage = new Storage(implementation, data.Name, data.Path, state, _services);
            _storages.Add(storage);
            AddedStorage?.Invoke(storage);
            return storage;
        }

        public IModelRepository AddRepository(string type, string url, string name, PresetSettings settings)
        {
            var types = _services.Get<Types>();
            if (!types.GetRepoTypes().Contains(type)) throw new ArgumentException();
            var (entry, repoState) = PersistentState.AddRepo(name, url, type, settings);
            return CreateRepository(entry, repoState);
        }

        public IModelStorage AddStorage(string type, string path, string name)
        {
            var types = _services.Get<Types>();
            if (!types.GetStorageTypes().Contains(type)) throw new ArgumentException();
            var (entry, storageState) = PersistentState.AddStorage(name, path, type);
            return CreateStorage(entry, storageState);
        }

        public IEnumerable<IModelStorage> GetStorages() => _storages;

        public IEnumerable<IModelRepository> GetRepositories() => _repositories;
        public void ConnectErrorPresenter(IErrorPresenter presenter)
        {
            _errorPresenter.Connect(presenter);
        }

        public async Task<ServerInfo?> CheckRepositoryUrl(string url, CancellationToken cancellationToken)
        {
            var types = _services.Get<Types>();
            return await types.CheckUrl(url, cancellationToken);
        }

        public void DeleteRepository(IModelRepository repository, bool removeMods)
        {
            if (removeMods) throw new NotImplementedException();
            _repositories.Remove(repository);
            RemovedRepository?.Invoke(repository);
            PersistentState.RemoveRepository(repository.Identifier);
            // TODO: dispose / stop actions
        }

        public void DeleteStorage(IModelStorage storage, bool removeMods)
        {
            storage.Delete(removeMods);
            _storages.Remove(storage);
            RemovedStorage?.Invoke(storage);
            PersistentState.RemoveStorage(storage.Identifier);
            // TODO: dispose / stop actions
        }
    }
}
