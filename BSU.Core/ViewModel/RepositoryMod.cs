using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.Core.Model.Updating;
using BSU.Core.Sync;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class RepositoryMod : ObservableBase
    {
        internal readonly IModelRepositoryMod Mod;
        private readonly IViewModelService _viewModelService;

        public string Name { get; }

        public string DisplayName { private set; get; }

        private string _downloadIdentifier = "";

        public FileSyncProgress UpdateProgress { get; } = new();

        public ModActionTree Actions { get; } = new();

        private void SetSelectionFromView(ModAction value)
        {
            Mod.SetSelection(value.AsSelection);
            _viewModelService.Update(); // TODO: await? pls? somewhere? :(
        }

        internal RepositoryMod(IModelRepositoryMod mod, IModel model, IViewModelService viewModelService)
        {
            Actions.SelectionChanged += () => SetSelectionFromView(Actions.Selection);
            Mod = mod;
            _viewModelService = viewModelService;
            Name = mod.Identifier;

            DownloadIdentifier = mod.DownloadIdentifier;

            foreach (var storage in model.GetStorages())
            {
                AddStorage(storage);
            }
        }

        private async Task<ModAction> UpdateAction(IModelStorageMod storageMod)
        {
            var action = await CoreCalculation.GetModAction(Mod, storageMod, CancellationToken.None);
            if (action == ModActionEnum.Unusable)
            {
                Actions.RemoveMod(storageMod);
                return Actions.Selection;
            }
            var selection = new SelectMod(storageMod, action);
            Actions.UpdateMod(selection);
            return selection;
        }

        internal void AddStorage(IModelStorage storage)
        {
            if (!storage.CanWrite) return;
            Actions.AddStorage(storage);
        }

        internal void RemoveStorage(IModelStorage storage)
        {
            Actions.RemoveStorage(storage);
        }

        private async Task UpdateErrorText()
        {
            // TODO: make sure it updates itself when e.g. conflict states change

            if (Actions.Selection == null)
            {
                ErrorText = "Select an action";
                return;
            }

            if (Actions.Selection is SelectDoNothing)
            {
                ErrorText = "";
            }

            if (Actions.Selection is SelectStorage selectStorage)
            {
                var folderExists = await selectStorage.DownloadStorage.HasMod(DownloadIdentifier);
                ErrorText = folderExists ? "Name in use" : "";
            }

            if (Actions.Selection is SelectMod selectMod)
            {
                var conflicts = await Mod.GetConflictsUsingMod(selectMod.StorageMod, CancellationToken.None);
                if (!conflicts.Any())
                {
                    ErrorText = "";
                    return;
                }

                var conflictNames = conflicts.Select(c => $"{c}");
                ErrorText = "Conflicts: " + string.Join(", ", conflictNames);
            }
        }

        // TODO: validate folder name: invalid chars, leading '@'
        public string DownloadIdentifier
        {
            get => _downloadIdentifier;
            set
            {
                if (value == _downloadIdentifier) return;
                Mod.DownloadIdentifier = value;
                _downloadIdentifier = value;
                OnPropertyChanged();
                UpdateErrorText(); // TODO: await :(
            }
        }

        private string _errorText;
        private bool _canChangeSelection = true;

        public string ErrorText
        {
            get => _errorText;
            private set
            {
                if (value == _errorText) return;
                _errorText = value;
                OnPropertyChanged();
            }
        }

        public bool CanChangeSelection
        {
            get => _canChangeSelection;
            set
            {
                if (_canChangeSelection == value) return;
                _canChangeSelection = value;
                OnPropertyChanged();
            }
        }

        public async Task Load()
        {
            DisplayName = await Mod.GetDisplayName(CancellationToken.None);
        }

        public async Task Update()
        {
            var selection = await Mod.GetSelection(CancellationToken.None);
            if (selection is RepositoryModActionStorageMod actionStorageMod)
            {
                var updatedAction = await UpdateAction(actionStorageMod.StorageMod);
                Actions.SetSelection(updatedAction);
            }
            else
            {
                var action = await ModAction.Create(selection, Mod, CancellationToken.None);
                Actions.SetSelection(action);
            }

            await UpdateErrorText();

            var actions = await Mod.GetModActions(CancellationToken.None);
            foreach (var (mod, _) in actions)
            {
                await UpdateAction(mod);
            }
        }

        internal async Task<(IModUpdate update, Progress<FileSyncStats> progress)> StartUpdate(CancellationToken cancellationToken)
        {
            var progress = UpdateProgress.Progress;
            var update = await Mod.StartUpdate(progress, cancellationToken);
            if (update == null) return default;
            await _viewModelService.Update();

            return (update, progress);
        }
    }
}
