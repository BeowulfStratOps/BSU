﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using BSU.Core.Annotations;
using BSU.Core.Model;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class RepositoryMod : ViewModelClass
    {
        private readonly IModelRepositoryMod _mod;

        public string Name { get; }
        public string DisplayName { private set; get; }

        public bool IsLoading { private set; get; }

        private string _downloadIdentifier = "";
        private bool _showDownloadIdentifier;

        public ModActionTree Actions { get; } = new ModActionTree();

        private ModAction _selection;
        public ModAction Selection
        {
            get => _selection;
            set
            {
                if (_selection == value) return;
                _selection = value;
                _mod.Selection = value?.AsSelection;
                ShowDownloadIdentifier = _selection is SelectStorage;
                OnPropertyChanged();
                UpdateErrorText();
            }
        }

        internal RepositoryMod(IModelRepositoryMod mod, IModelStructure structure)
        {
            IsLoading = mod.GetState().IsLoading;
            _mod = mod;
            Name = mod.Identifier;
            mod.ActionAdded += AddAction;
            foreach (var target in mod.Actions.Keys)
            {
                AddAction(target);
            }

            Selection = ModAction.Create(mod.Selection, mod.Actions);
            DownloadIdentifier = mod.DownloadIdentifier;
            mod.SelectionChanged += () =>
            {
                Selection = ModAction.Create(mod.Selection, mod.Actions);
            };
            mod.DownloadIdentifierChanged += () =>
            {
                DownloadIdentifier = mod.DownloadIdentifier;
            };
            mod.StateChanged += () =>
            {
                DisplayName = mod.Implementation.GetDisplayName();
                OnPropertyChanged(nameof(DisplayName));
                IsLoading = mod.GetState().IsLoading;
                OnPropertyChanged(nameof(IsLoading));
            };
            foreach (var storage in structure.GetStorages())
            {
                AddStorage(storage);
            }
            SelectionChanged = new DelegateCommand(ChangeSelection);
        }

        private void AddAction(IModelStorageMod storageMod)
        {
            Actions.Add(new SelectMod(storageMod, _mod.Actions[storageMod]));
        }

        internal void AddStorage(IModelStorage storage)
        {
            if (!storage.CanWrite) return; // TODO
            Actions.Add(new SelectStorage(storage));
        }

        internal void RemoveStorage(IModelStorage storage)
        {
            if (!storage.CanWrite) return;
            var selection = Actions.OfType<SelectStorage>().Single(s => s.DownloadStorage == storage);
            Actions.Remove(selection);
        }

        private void ChangeSelection()
        {
            //_mod.Selection = _selection?.Selection;
        }

        private void UpdateErrorText()
        {
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
                var inUse = _mod.Selection.DownloadStorage.HasMod(DownloadIdentifier);
                ErrorText = inUse ? "Name in use" : "";
            }
            
            if (_mod.Selection.StorageMod != null)
            {
                var action = _mod.Actions[_mod.Selection.StorageMod];
                if (!action.Conflicts.Any())
                {
                    ErrorText = "";
                    return;
                }

                var conflicts = action.Conflicts.Select(c => $"{c.Parent}:{c}"); // TODO: include repo identifier
                ErrorText = "Conflicts: " + string.Join(", ", conflicts);
            }
        }

        public DelegateCommand SelectionChanged { get; }

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
    }
}
