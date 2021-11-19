using System.Linq;
using BSU.Core.Model;
using NLog;

namespace BSU.Core.Services
{
    internal class AutoSelector
    {
        private readonly IModel _model;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public AutoSelector(IModel model, StructureEventCombiner eventProvider)
        {
            _model = model;
            eventProvider.AnyChange += OnAnyChange;
        }

        private void OnAnyChange()
        {
            foreach (var mod in _model.GetRepositories().Where(r => r.State == LoadingState.Loaded)
                .SelectMany(r => r.GetMods()))
            {
                // TODO: check if the current selection was deleted
                if (mod.GetCurrentSelection() != null) continue;
                var selection = GetSelection(mod);
                if (selection != null)
                    mod.SetSelection(selection);
            }
        }

        private RepositoryModActionSelection GetSelection(IModelRepositoryMod mod)
        {
            /*if (Selection != null && !reset)
            {
                var deleted = Selection is RepositoryModActionDownload download && download.DownloadStorage.IsDeleted ||
                              Selection is RepositoryModActionStorageMod storageMod && storageMod.StorageMod.IsDeleted;
                if (!deleted) return Selection;
            }*/

            _logger.Trace("Checking auto-selection for mod {0}", mod.Identifier);

            var storageMods = _model.GetStorageMods().ToList();

            var previouslySelectedMod =
                storageMods.SingleOrDefault(m => m.GetStorageModIdentifiers().Equals(mod.GetPreviousSelection()));

            if (previouslySelectedMod != null)
                return new RepositoryModActionStorageMod(previouslySelectedMod);

            // TODO: check previously selected storage for download?

            var selectedMod = CoreCalculation.AutoSelect(mod, storageMods, _model.GetRepositoryMods());

            if (selectedMod != null)
                return new RepositoryModActionStorageMod(selectedMod);

            var storage = (_model.GetStorages().Where(s => s.CanWrite && s.IsAvailable())).FirstOrDefault();
            if (storage != null)
            {

                mod.DownloadIdentifier = Helper.GetAvailableDownloadIdentifier(storage, mod.Identifier);
                return new RepositoryModActionDownload(storage);
            }

            return null;
        }
    }
}
