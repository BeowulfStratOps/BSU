﻿using System;
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

        public string Name => _name;

        public string DisplayName
        {
            private set => _displayName = value;
            get => _displayName;
        }

        private string _downloadIdentifier = "";
        private bool _showDownloadIdentifier;

        public FileSyncProgress UpdateProgress => _updateProgress;

        public ModActionTree Actions => _actions;

        private ModAction _selection;
        public ModAction Selection
        {
            get => _selection;
            set
            {
                if (value == null) return; // can happen when the collection is modified
                if (Equals(_selection, value)) return;
                _selection = value;
                Mod.SetSelection(value.AsSelection);
                ShowDownloadIdentifier = _selection is SelectStorage;
                OnPropertyChanged();
                UpdateErrorText(); // TODO: await? :(
                _viewModelService.Update(); // TODO: await? pls? somewhere? :(
            }
        }

        internal RepositoryMod(IModelRepositoryMod mod, IModel model, IViewModelService viewModelService)
        {
            Mod = mod;
            _viewModelService = viewModelService;
            _name = mod.Identifier;

            DownloadIdentifier = mod.DownloadIdentifier;

            foreach (var storage in model.GetStorages())
            {
                AddStorage(storage);
            }

            UpdateProgress.Progress.ProgressChanged += (_, stats) =>
            {
                if (stats.State == FileSyncState.None) CanChangeSelection = true;
            };
        }

        private async Task<ModAction> UpdateAction(IModelStorageMod storageMod)
        {
            var isCurrentlySelected = Selection?.AsSelection is RepositoryModActionStorageMod actionStorageMod && actionStorageMod.StorageMod == storageMod;
            var action = await CoreCalculation.GetModAction(Mod, storageMod, CancellationToken.None);
            if (action == ModActionEnum.Unusable)
            {
                var removeAction = Actions.SingleOrDefault(a => a.AsSelection is RepositoryModActionStorageMod storageModSelection && storageModSelection.StorageMod == storageMod);
                if (isCurrentlySelected)
                    Selection = new SelectDoNothing();
                if (removeAction != null)
                    Actions.Remove(removeAction);
                return Selection;
            }
            var selection = new SelectMod(storageMod, action);
            Actions.Update(selection);
            if (isCurrentlySelected)
                Selection = selection;
            return selection;
        }

        internal void AddStorage(IModelStorage storage)
        {
            if (!storage.CanWrite) return; // TODO
            Actions.Update(new SelectStorage(storage));
        }

        internal void RemoveStorage(IModelStorage storage)
        {
            if (!storage.CanWrite) return;
            var selection = Actions.OfType<SelectStorage>().Single(s => s.DownloadStorage == storage);
            Actions.Remove(selection);
        }

        private async Task UpdateErrorText()
        {
            // TODO: make sure it updates itself when e.g. conflict states change

            if (Selection == null)
            {
                ErrorText = "Select an action";
                return;
            }

            if (Selection is SelectDoNothing)
            {
                ErrorText = "";
            }

            if (Selection is SelectStorage selectStorage)
            {
                var folderExists = await selectStorage.DownloadStorage.HasMod(DownloadIdentifier);
                ErrorText = folderExists ? "Name in use" : "";
            }

            if (Selection is SelectMod selectMod)
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

        public bool ShowDownloadIdentifier
        {
            get => _showDownloadIdentifier;
            private set
            {
                if (value == _showDownloadIdentifier) return;
                _showDownloadIdentifier = value;
                OnPropertyChanged();
            }
        }

        private string _errorText;
        private readonly string _name;
        private string _displayName;
        private readonly FileSyncProgress _updateProgress = new();
        private readonly ModActionTree _actions = new();
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
                Selection = updatedAction;
            }
            else
            {
                Selection = await ModAction.Create(selection, Mod, CancellationToken.None);
            }

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
            CanChangeSelection = false;
            await _viewModelService.Update();

            return (update, progress);
        }
    }
}
