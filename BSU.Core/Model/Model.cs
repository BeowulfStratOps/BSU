using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Persistence;
using BSU.Core.Storage;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class Model : IModel
    {
        private readonly Types _types;

        private readonly ModelStructure _structure = new();
        private readonly ErrorPresenter _errorPresenter = new();

        private InternalState PersistentState { get; }
        private ILogger _logger = LogManager.GetCurrentClassLogger();

        public Model(InternalState persistentState, Types types)
        {
            _types = types;
            PersistentState = persistentState;
            if (PersistentState.CheckIsFirstStart())
            {
                DoFirstStartSetup();
            }
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
            PersistentState.AddStorage("Steam", new DirectoryInfo(steamPath), "STEAM");
        }

        public void Load()
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
            var implementation = _types.GetRepoImplementation(data.Type, data.Url);
            var repository = new Repository(implementation, data.Name, data.Url, state, _structure, _errorPresenter);
            // TODO: kick off mods
            _structure.AddRepository(repository);
            return repository;
        }

        private Storage CreateStorage(IStorageEntry data, IStorageState state)
        {
            var implementation = _types.GetStorageImplementation(data.Type, data.Path);
            var storage = new Storage(implementation, data.Name, data.Path, state, _structure, _errorPresenter);
            // TODO: kick off mods
            _structure.AddStorage(storage);
            return storage;
        }

        public IModelRepository AddRepository(string type, string url, string name)
        {
            if (!_types.GetRepoTypes().Contains(type)) throw new ArgumentException();
            var (entry, repoState) = PersistentState.AddRepo(name, url, type);
            return CreateRepository(entry, repoState);
        }

        public IModelStorage AddStorage(string type, DirectoryInfo dir, string name)
        {
            if (!_types.GetStorageTypes().Contains(type)) throw new ArgumentException();
            var (entry, storageState) = PersistentState.AddStorage(name, dir, type);
            return CreateStorage(entry, storageState);
        }

        public IEnumerable<IModelStorage> GetStorages() => _structure.GetStorages();

        public IEnumerable<IModelRepository> GetRepositories() => _structure.GetRepositories();
        public void ConnectErrorPresenter(IErrorPresenter presenter)
        {
            _errorPresenter.Connect(presenter);
        }

        public async Task<ServerInfo> CheckRepositoryUrl(string url, CancellationToken cancellationToken)
        {
            return await _types.CheckUrl(url, cancellationToken);
        }

        public void DeleteRepository(IModelRepository repository, bool removeMods)
        {
            if (removeMods) throw new NotImplementedException();
            _structure.RemoveRepository(repository);
            PersistentState.RemoveRepository(repository.Identifier);
            // TODO: dispose / stop actions
        }

        public void DeleteStorage(IModelStorage storage, bool removeMods)
        {
            if (removeMods) throw new NotImplementedException();
            _structure.RemoveStorage(storage);
            PersistentState.RemoveStorage(storage.Identifier);
            // TODO: dispose / stop actions
        }
    }
}
