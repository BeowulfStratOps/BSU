using System.Linq;
using BSU.Core.Model;
using NLog;

namespace BSU.Core.Services
{
    internal class AutoSelector
    {
        private readonly IModel _model;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public AutoSelector(IModel model)
        {
            _model = model;
            _model.AnyChange += OnAnyChange;
        }

        private void OnAnyChange()
        {
            foreach (var mod in _model.GetRepositories().Where(r => r.State == LoadingState.Loaded)
                .SelectMany(r => r.GetMods()))
            {
                var currentSelection = mod.GetCurrentSelection();
                var deleted = currentSelection is RepositoryModActionDownload download && download.DownloadStorage.IsDeleted ||
                              currentSelection is RepositoryModActionStorageMod storageMod && storageMod.StorageMod.IsDeleted;

                if (currentSelection != null && !deleted) continue;
                var selection = GetSelection(mod);
                if (selection != null)
                    mod.SetSelection(selection);
            }
        }

        private RepositoryModActionSelection GetSelection(IModelRepositoryMod mod)
        {
            _logger.Trace("Checking auto-selection for mod {0}", mod.Identifier);

            var storageMods = _model.GetStorageMods().ToList();

            var previouslySelectedMod =
                storageMods.SingleOrDefault(m => m.GetStorageModIdentifiers().Equals(mod.GetPreviousSelection()));

            if (previouslySelectedMod != null)
                return new RepositoryModActionStorageMod(previouslySelectedMod);

            // TODO: check previously selected storage for download?

            // wait for everything to load.
            if (_model.GetStorages().Any(s => s.State == LoadingState.Loading)) return null;
            if (_model.GetRepositories().Any(s => s.State == LoadingState.Loading)) return null;
            if (_model.GetRepositoryMods().Any(s => s.State == LoadingState.Loading)) return null;
            if (_model.GetStorageMods().Any(s => s.GetState() == StorageModStateEnum.Loading)) return null;

            var selectedMod = CoreCalculation.AutoSelect(mod, storageMods, _model.GetRepositoryMods());

            if (selectedMod != null)
                return new RepositoryModActionStorageMod(selectedMod);

            var storage = _model.GetStorages().FirstOrDefault(s => s.CanWrite && s.IsAvailable());
            if (storage != null)
            {

                mod.DownloadIdentifier = CoreCalculation.GetAvailableDownloadIdentifier(storage, mod.Identifier);
                return new RepositoryModActionDownload(storage);
            }

            return null;
        }
    }
}
