using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BSU.Core.Model;
using BSU.Core.Services;

namespace BSU.Core.ViewModel.Util
{
    public class ModActionTree : ObservableBase
    {
        public ObservableCollection<IActionListEntry> Storages { get; } = new();
        private bool _isOpen;

        private ModAction _selection = new SelectLoading();
        private readonly IModel _model;
        private readonly IModelRepositoryMod _repoMod;

        public ModAction Selection
        {
            get => _selection;
            private set
            {
                if (Equals(value, _selection)) return;
                _selection = value;
                OnPropertyChanged();
            }
        }

        public bool IsOpen
        {
            get => _isOpen;
            set
            {
                if (_isOpen == value) return;
                _isOpen = value;
                OnPropertyChanged();
            }
        }

        public event Action? SelectionChanged;

        public DelegateCommand Open { get; }

        private bool _canOpen = true;
        public bool CanOpen
        {
            get => _canOpen;
            set
            {
                if (SetProperty(ref _canOpen, value))
                    Open.SetCanExecute(value);
            }
        }

        internal ModActionTree(IModelRepositoryMod repoMod, IModel model)
        {
            _repoMod = repoMod;
            _model = model;
            Open = new DelegateCommand(() => IsOpen = true);
            Update();
        }

        public void Update()
        {
            var currentSelection = _repoMod.GetCurrentSelection();
            Selection = ModAction.Create(currentSelection, _repoMod, SetSelection);
            Storages.Clear();
            Storages.Add(new SelectableModAction(new SelectDisabled(), SetSelection,
                false, true));

            foreach (var storage in _model.GetStorages())
            {
                if (storage.State != LoadingState.Loaded) continue;

                var actions = new List<SelectableModAction>();

                foreach (var mod in storage.GetMods())
                {
                    var actionType = CoreCalculation.GetModAction(_repoMod, mod);
                    if (actionType == ModActionEnum.Unusable) continue;
                    var action = new SelectMod(mod, actionType);
                    var isSelected = _repoMod.GetCurrentSelection() is ModSelectionStorageMod storageMod &&
                                     storageMod.StorageMod == mod;
                    var enabled = (storage.CanWrite && actionType != ModActionEnum.Loading) || actionType == ModActionEnum.Use;
                    actions.Add(new SelectableModAction(action, SetSelection, isSelected, enabled));
                }

                if (actions.Any() || storage.CanWrite)
                {
                    var isSelected = false;
                    var downloadName = _repoMod.Identifier;
                    if (_repoMod.GetCurrentSelection() is ModSelectionDownload download)
                    {
                        isSelected = download.DownloadStorage == storage;
                        downloadName = download.DownloadName;
                    }
                    Storages.Add(new StorageModActionList(storage, downloadName, SetSelection, actions, isSelected));
                }
            }
        }

        private void SetSelection(ModAction action)
        {
            IsOpen = false;
            Selection = action;
            SelectionChanged?.Invoke();
        }
    }

    public interface IActionListEntry
    {
    }

    public class StorageModActionList : ObservableBase, IActionListEntry
    {
        internal IModelStorage Storage { get; }

        public string Name => Storage.Name;

        public ObservableCollection<SelectableModAction> Mods { get; } = new();

        public string Path => Storage.GetLocation();

        internal StorageModActionList(IModelStorage storage, string downloadName, Action<ModAction> selectStorage,
            List<SelectableModAction> actions, bool isSelected)
        {
            Storage = storage;
            if (storage.CanWrite)
                Mods.Add(new SelectableModAction(new SelectStorage(storage, downloadName, selectStorage), selectStorage, isSelected, true));
            foreach (var action in actions)
            {
                Mods.Add(action);
            }
        }
    }

    public class SelectableModAction : ObservableBase, IActionListEntry
    {
        public SelectableModAction(ModAction action, Action<ModAction> select, bool isSelected, bool isEnabled)
        {
            _select = select;
            _isSelected = isSelected;
            IsEnabled = isEnabled;
            Action = action;
        }

        public ModAction Action { get; }
        private readonly Action<ModAction> _select;
        private bool _isSelected;
        public bool IsEnabled { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public void Select()
        {
            _select(Action);
        }
    }
}
