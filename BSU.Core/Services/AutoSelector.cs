using System;
using System.Linq;
using BSU.Core.Events;
using BSU.Core.Ioc;
using BSU.Core.Model;
using NLog;

namespace BSU.Core.Services
{
    internal class AutoSelector
    {
        private readonly IModel _model;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public AutoSelector(IServiceProvider serviceProvider)
        {
            _model = serviceProvider.Get<IModel>();
            var eventManager = serviceProvider.Get<IEventManager>();
            eventManager.Subscribe<AnythingChangedEvent>(OnAnyChange);
        }

        private void OnAnyChange(AnythingChangedEvent evt)
        {
            foreach (var mod in _model.GetRepositories().Where(r => r.State == LoadingState.Loaded)
                .SelectMany(r => r.GetMods()))
            {
                var currentSelection = mod.GetCurrentSelection();
                var deleted = currentSelection is ModSelectionDownload download && download.DownloadStorage.IsDeleted ||
                              currentSelection is ModSelectionStorageMod storageMod && storageMod.StorageMod.IsDeleted;

                if (currentSelection is not ModSelectionNone and not ModSelectionLoading && !deleted) continue;
                var selection = GetSelection(mod);
                mod.SetSelection(selection);
            }
        }

        private ModSelection GetSelection(IModelRepositoryMod mod)
        {
            _logger.Trace("Checking auto-selection for mod {0}", mod.Identifier);

            var storageMods = _model.GetStorageMods().ToList();

            var previouslySelectedMod =
                storageMods.SingleOrDefault(m => m.GetStorageModIdentifiers().Equals(mod.GetPreviousSelection()));

            if (previouslySelectedMod != null)
                return new ModSelectionStorageMod(previouslySelectedMod);

            // TODO: check previously selected storage for download?

            // wait for everything to load.
            if (_model.GetStorages().Any(s => s.State == LoadingState.Loading)) return new ModSelectionLoading();
            if (_model.GetRepositories().Any(s => s.State == LoadingState.Loading)) return new ModSelectionLoading();
            if (_model.GetRepositoryMods().Any(s => s.State == LoadingState.Loading)) return new ModSelectionLoading();
            if (_model.GetStorageMods().Any(s => s.GetState() == StorageModStateEnum.Loading)) return new ModSelectionLoading();

            var selectedMod = CoreCalculation.AutoSelect(mod, storageMods, _model.GetRepositoryMods());

            if (selectedMod != null)
                return new ModSelectionStorageMod(selectedMod);

            var storage = _model.GetStorages().FirstOrDefault(s => s.CanWrite && s.IsAvailable());
            if (storage != null)
            {

                mod.DownloadIdentifier = CoreCalculation.GetAvailableDownloadIdentifier(storage, mod.Identifier);
                return new ModSelectionDownload(storage);
            }

            return new ModSelectionNone();
        }
    }
}
