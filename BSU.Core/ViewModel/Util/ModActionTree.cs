using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
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
                SelectionChanged?.Invoke();
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
            Selection = ModAction.Create(currentSelection, _repoMod);
            Storages.Clear();
            Storages.Add(new SelectableModAction(new SelectDisabled(), SetSelection,
                false));

            foreach (var storage in _model.GetStorages())
            {
                if (storage.State != LoadingState.Loaded) continue;

                var actions = new List<SelectableModAction>();

                foreach (var mod in storage.GetMods())
                {
                    var actionType = CoreCalculation.GetModAction(_repoMod, mod);
                    if (actionType != ModActionEnum.Unusable)
                    {
                        var action = new SelectMod(mod, actionType);
                        var isSelected = _repoMod.GetCurrentSelection() is ModSelectionStorageMod storageMod &&
                                         storageMod.StorageMod == mod;
                        actions.Add(new SelectableModAction(action, SetSelection, isSelected));
                    }
                }

                if (actions.Any() || storage.CanWrite)
                {
                    var isSelected = _repoMod.GetCurrentSelection() is ModSelectionDownload download &&
                                     download.DownloadStorage == storage;
                    Storages.Add(new StorageModActionList(storage, SetSelection, actions, isSelected));
                }
            }
        }

        private void SetSelection(ModAction action)
        {
            Selection = action;
            IsOpen = false;
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

        internal StorageModActionList(IModelStorage storage, Action<ModAction> selectStorage,
            List<SelectableModAction> actions, bool isSelected)
        {
            Storage = storage;
            if (!storage.CanWrite) return;
            Mods.Add(new SelectableModAction(new SelectStorage(storage), selectStorage, isSelected));
            foreach (var action in actions)
            {
                Mods.Add(action);
            }
        }
    }

    public class SelectableModAction : ObservableBase, IActionListEntry
    {
        public SelectableModAction(ModAction action, Action<ModAction> select, bool isSelected)
        {
            _select = select;
            _isSelected = isSelected;
            Action = action;
        }

        public ModAction Action { get; }
        private readonly Action<ModAction> _select;
        private bool _isSelected;

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
