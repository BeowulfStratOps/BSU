using System;
using BSU.Core.Concurrency;
using BSU.Core.Events;
using BSU.Core.Ioc;
using BSU.Core.Model;
using NLog;

namespace BSU.Core.Services
{
    internal class StructureEventCombiner
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IEventManager _eventManager;

        public StructureEventCombiner(IServiceProvider serviceProvider, IModel model)
        {
            _eventManager = serviceProvider.Get<IEventManager>();
            model.AddedRepository += AddedRepository;
            model.AddedStorage += AddedStorage;
            model.RemovedRepository += _ => OnAnyChange();
            model.RemovedStorage += _ => OnAnyChange();
        }

        private void OnAnyChange()
        {
            _eventManager.Publish(new AnythingChangedEvent());
        }

        private void AddedStorage(IModelStorage storage)
        {
            _logger.Trace($"Added storage {storage.Name}");
            storage.StateChanged += CheckStorage;
            CheckStorage(storage);
        }

        private void CheckStorage(IModelStorage storage)
        {
            if (storage.State != LoadingState.Loaded) return;
            storage.StateChanged -= CheckStorage;

            foreach (var mod in storage.GetMods())
            {
                AddStorageMod(mod);
            }

            storage.AddedMod += AddStorageMod;

            OnAnyChange();
        }

        private void AddStorageMod(IModelStorageMod mod)
        {
            _logger.Trace($"Added storage mod {mod.ParentStorage.Name}/{mod.Identifier}");
            mod.StateChanged += _ => OnAnyChange();
            OnAnyChange();
        }

        private void AddedRepository(IModelRepository repository)
        {
            _logger.Trace($"Added repo {repository.Name}");
            repository.StateChanged += CheckRepository;
        }

        private void CheckRepository(IModelRepository repository)
        {
            if (repository.State != LoadingState.Loaded) return;
            repository.StateChanged -= CheckRepository;

            foreach (var mod in repository.GetMods())
            {
                AddRepositoryMod(mod);
            }
        }

        private void AddRepositoryMod(IModelRepositoryMod mod)
        {
            _logger.Trace($"Added repository mod {mod.ParentRepository.Name}/{mod.Identifier}");
            mod.StateChanged += _ => OnAnyChange();
            mod.SelectionChanged += _ => OnAnyChange();
            mod.DownloadIdentifierChanged += _ => OnAnyChange();
            OnAnyChange();
        }
    }
}
