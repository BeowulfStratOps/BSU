using System;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.Core.Model.Utility;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class RepositoryMod : ObservableBase
    {
        private readonly IModelRepositoryMod _mod;

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
                _mod.Selection = value.AsSelection;
                ShowDownloadIdentifier = _selection is SelectStorage;
                OnPropertyChanged();
                UpdateErrorText();
            }
        }

        internal RepositoryMod(IModelRepositoryMod mod, IModelStructure structure, IModelStructure modelStructure)
        {
            //IsLoading = mod.GetState().IsLoading;
            _mod = mod;
            _modelStructure = modelStructure;
            Name = mod.Identifier;
            mod.LocalModUpdated += UpdateAction;

            Selection = ModAction.Create(mod.Selection, mod);
            DownloadIdentifier = mod.DownloadIdentifier;
            mod.SelectionChanged += () =>
            {
                Selection = ModAction.Create(mod.Selection, mod);
                DownloadIdentifier = mod.DownloadIdentifier;
            };
            DisplayName = mod.GetDisplayName();
            OnPropertyChanged(nameof(DisplayName));
            foreach (var storage in structure.GetStorages())
            {
                AddStorage(storage);
            }
        }

        private void UpdateAction(IModelStorageMod storageMod)
        {
            var reSelect = Selection?.AsSelection?.StorageMod == storageMod;
            var selection = new SelectMod(storageMod, (ModActionEnum) CoreCalculation.GetModAction(_mod, storageMod));
            Actions.Update(selection);
            if (reSelect)
                Selection = selection;
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

        private void UpdateErrorText()
        {
            // TODO: make sure it updates itself when e.g. conflict states change

            if (_mod.Selection == null)
            {
                ErrorText = "Select an action";
                return;
            }

            if (_mod.Selection.DoNothing)
            {
                ErrorText = "";
            }

            if (_mod.Selection.DownloadStorage != null)
            {
                _mod.Selection.DownloadStorage.HasMod(DownloadIdentifier)
                    .ContinueWith(async inUse => ErrorText = await inUse ? "Name in use" : "");

            }

            if (_mod.Selection.StorageMod != null)
            {
                var conflicts = CoreCalculation.GetConflicts(_mod, _mod.Selection.StorageMod, _modelStructure);
                if (!conflicts.Any())
                {
                    ErrorText = "";
                    return;
                }

                var conflictNames = conflicts.Select(c => $"{c}");
                ErrorText = "Conflicts: " + string.Join(", ", conflicts);
            }
        }

        // TODO: validate folder name: invalid chars, leading '@'
        public string DownloadIdentifier
        {
            get => _downloadIdentifier;
            set
            {
                if (value == _downloadIdentifier) return;
                _mod.DownloadIdentifier = value;
                _downloadIdentifier = value;
                OnPropertyChanged();
                UpdateErrorText();
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

        public IProgressProvider UpdateProgress
        {
            get => _updateProgress;
            private set
            {
                if (value == _updateProgress) return;
                _updateProgress = value;
                OnPropertyChanged();
            }
        }

        public double Progress { get; private set; }

        private string _errorText;
        private IProgressProvider _updateProgress;
        private readonly  IModelStructure _modelStructure;

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
    }
}
