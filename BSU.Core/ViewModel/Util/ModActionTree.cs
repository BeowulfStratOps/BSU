using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using BSU.Core.Model;

namespace BSU.Core.ViewModel.Util
{
    public class ModActionTree : ObservableBase
    {
        public ObservableCollection<IActionListEntry> Storages { get; } = new();
        private bool _isOpen;

        private ModAction _selection;
        public ModAction Selection
        {
            get => _selection;
            private set
            {
                if (value.Equals(_selection)) return;
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

        public event Action SelectionChanged;

        public void SetSelection(ModAction action)
        {
            Selection = action;
            SetIsSelected(action);
        }

        public ICommand Open { get; }

        public ModActionTree()
        {
            Open = new DelegateCommand(() => IsOpen = true);
            Storages.Add( new SelectableModAction(new SelectDoNothing(), this, false));
        }

        internal void UpdateMod(SelectMod mod)
        {
            FindStorageByMod(mod.StorageMod).UpdateMod(mod);
        }

        private StorageModActionList FindStorageByMod(IModelStorageMod mod)
        {
            var storageId = (Guid)mod.GetStorageModIdentifiers().Storage;
            return Storages.OfType<StorageModActionList>().Single(s => s.Storage.Identifier == storageId);
        }

        internal void AddStorage(IModelStorage storage)
        {
            Storages.Add(new StorageModActionList(storage, this));
        }

        internal void RemoveStorage(IModelStorage storage)
        {
            throw new NotImplementedException();
        }

        internal void RemoveMod(IModelStorageMod mod)
        {
            FindStorageByMod(mod).RemoveMod(mod);
        }

        public void Select(ModAction action)
        {
            IsOpen = false;
            SetIsSelected(action);
            if (Selection.Equals(action)) return;
            Selection = action;
            SelectionChanged?.Invoke();
        }

        private void SetIsSelected(ModAction action)
        {
            foreach (var listEntry in Storages)
            {
                if (listEntry is SelectableModAction selectableAction)
                    selectableAction.IsSelected = selectableAction.Action.Equals(action);
                if (listEntry is not StorageModActionList list) continue;
                foreach (var selectableModAction in list.Mods)
                {
                    selectableModAction.IsSelected = selectableModAction.Action.Equals(action);
                }
            }
        }
    }

    public interface IActionListEntry
    {
    }

    public class StorageModActionList : IActionListEntry
    {
        internal IModelStorage Storage { get; }
        private ModActionTree Parent;

        public string Name => Storage.Name;

        public ObservableCollection<SelectableModAction> Mods { get; } = new();

        internal StorageModActionList(IModelStorage storage, ModActionTree parent)
        {
            Parent = parent;
            Storage = storage;
            Mods.Add( new SelectableModAction(new SelectStorage(storage), Parent, false));
        }

        public void UpdateMod(SelectMod mod)
        {
            var index = FindIndex(mod.StorageMod);

            if (index == -1)
            {
                Mods.Insert(0, new SelectableModAction(mod, Parent, false));
                return;
            }

            Mods[index] = new SelectableModAction(mod, Parent, Mods[index].IsSelected);
        }

        private int FindIndex(IModelStorageMod mod)
        {
            var index = -1;
            for (var i = 0; i < Mods.Count; i++)
            {
                if (Mods[i].Action is not SelectMod selectMod || selectMod.StorageMod != mod) continue;
                index = i;
                break;
            }

            return index;
        }

        internal void RemoveMod(IModelStorageMod mod)
        {
            var index = FindIndex(mod);
            if (index != -1) Mods.RemoveAt(index);
        }
    }

    public class SelectableModAction : ObservableBase, IActionListEntry
    {
        private readonly ModActionTree _parent;

        public SelectableModAction(ModAction action, ModActionTree parent, bool isSelected)
        {
            _isSelected = isSelected;
            _parent = parent;
            Action = action;
        }

        public ModAction Action { get; }
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
            IsSelected = true;
            _parent.Select(Action);
        }
    }
}
