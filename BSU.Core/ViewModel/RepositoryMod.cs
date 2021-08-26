using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class RepositoryMod : ObservableBase
    {
        internal readonly IModelRepositoryMod Mod;

        public string Name { get; }
        public string DisplayName { private set; get; }

        private string _downloadIdentifier = "";
        private bool _showDownloadIdentifier;

        public ModActionTree Actions { get; } = new();

        private ModAction _selection;
        public ModAction Selection
        {
            get => _selection;
            set
            {
                if (_selection == value) return;
                _selection = value;
                if (value == null) return; // can't be done by user.
                //Mod.Selection = value.AsSelection;
                ShowDownloadIdentifier = _selection is SelectStorage;
                OnPropertyChanged();
                //UpdateErrorText();
            }
        }

        internal RepositoryMod(IModelRepositoryMod mod, IModel model)
        {
            //IsLoading = mod.GetState().IsLoading;
            Mod = mod;
            Name = mod.Identifier;

            DownloadIdentifier = mod.DownloadIdentifier;

            OnPropertyChanged(nameof(DisplayName));
            foreach (var storage in model.GetStorages())
            {
                AddStorage(storage);
            }
        }

        private async Task<ModAction> UpdateAction(IModelStorageMod storageMod)
        {
            var isCurrentlySelected = Selection?.AsSelection?.StorageMod == storageMod;
            var action = await CoreCalculation.GetModAction(Mod, storageMod, CancellationToken.None);
            if (action == ModActionEnum.Unusable)
            {
                var removeAction = Actions.SingleOrDefault(a => a.AsSelection.StorageMod == storageMod);
                if (removeAction != null)
                    Actions.Remove(removeAction);
                if (isCurrentlySelected)
                    Selection = new SelectDoNothing();
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

        /*private void UpdateErrorText()
        {
            // TODO: make sure it updates itself when e.g. conflict states change

            if (Mod.Selection == null)
            {
                ErrorText = "Select an action";
                return;
            }

            if (Mod.Selection.DoNothing)
            {
                ErrorText = "";
            }

            if (Mod.Selection.DownloadStorage != null)
            {
                Mod.Selection.DownloadStorage.HasMod(DownloadIdentifier)
                    .ContinueWith(async inUse => ErrorText = await inUse ? "Name in use" : "");

            }

            if (Mod.Selection.StorageMod != null)
            {
                var conflicts = CoreCalculation.GetConflicts(Mod, Mod.Selection.StorageMod, _modelStructure);
                if (!conflicts.Any())
                {
                    ErrorText = "";
                    return;
                }

                var conflictNames = conflicts.Select(c => $"{c}");
                ErrorText = "Conflicts: " + string.Join(", ", conflicts);
            }
        }*/

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
                //UpdateErrorText();
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

        public async Task Load()
        {
            DisplayName = await Mod.GetDisplayName(CancellationToken.None);
            var selection = await Mod.GetSelection(CancellationToken.None);
            if (selection.StorageMod != null)
            {
                var updatedAction = await UpdateAction(selection.StorageMod);
                Selection = updatedAction;
            }
            else
            {
                Selection = ModAction.Create(selection, Mod, null);
            }
        }
    }
}
