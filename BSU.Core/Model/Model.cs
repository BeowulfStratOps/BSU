using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Events;
using BSU.Core.Ioc;
using BSU.Core.Launch;
using BSU.Core.Persistence;
using BSU.Core.Services;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class Model : IModel
    {
        private readonly List<IModelRepository> _repositories = new();
        private readonly List<IModelStorage> _storages = new();

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
        public GlobalSettings GetSettings() => PersistentState.Settings;

        public void SetSettings(GlobalSettings globalSettings)
        {
            PersistentState.Settings = globalSettings;
            _services.Get<IEventManager>().Publish(new SettingsChangedEvent());
        }

        private IInternalState PersistentState { get; }
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly IServiceProvider _services;

        public Model(IInternalState persistentState, ServiceProvider services, bool isFirstStart)
        {
            PersistentState = persistentState;
            if (isFirstStart)
            {
                DoFirstStartSetup();
            }

            _services = services;

            // TODO: should they be registered somewhere?
            new AutoSelectionActor(_services, this);
            new StructureEventCombiner(_services, this);
        }

        private void DoFirstStartSetup()
        {
            _logger.Info("First start setup");
            PersistentState.Settings = GlobalSettings.BuildDefault();
        }

        public void Load()
        {
            if (PersistentState.GetStorages().All(e => e.Item1.Type != "STEAM"))
            {
                PersistentState.AddStorage("Steam", "steam", "STEAM");
            }
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
            var storage = new Storage(implementation, data.Name, state, _services);
            _storages.Add(storage);
            AddedStorage?.Invoke(storage);
            return storage;
        }

        public IModelRepository AddRepository(string type, string url, string name)
        {
            var types = _services.Get<Types>();
            if (!types.GetRepoTypes().Contains(type)) throw new ArgumentException($"Unknown type {type}", nameof(type));
            var (entry, repoState) = PersistentState.AddRepo(name, url, type);
            return CreateRepository(entry, repoState);
        }

        public IModelStorage AddStorage(string type, string path, string name)
        {
            var types = _services.Get<Types>();
            if (!types.GetStorageTypes().Contains(type)) throw new ArgumentException($"Unknown type {type}", nameof(type));
            var (entry, storageState) = PersistentState.AddStorage(name, path, type);
            return CreateStorage(entry, storageState);
        }

        public IEnumerable<IModelStorage> GetStorages() => _storages;

        public IEnumerable<IModelRepository> GetRepositories() => _repositories;

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
