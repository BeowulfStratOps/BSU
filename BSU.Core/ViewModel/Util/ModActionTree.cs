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

        public event Action SelectionChanged;

        public void SetSelection(ModAction action)
        {
            Selection = action;
            SetIsSelected(action);
        }

        public DelegateCommand Open { get; }

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
            var storage = Storages.OfType<StorageModActionList>().SingleOrDefault(s => s.Storage == mod.ParentStorage);
            if (storage == null && mod.ParentStorage.CanWrite)
                storage = AddStorage(mod.ParentStorage);
            return storage;
        }

        internal StorageModActionList AddStorage(IModelStorage storage)
        {
            var existing = Storages.OfType<StorageModActionList>().SingleOrDefault(s => s.Storage == storage);
            if (existing != null) return existing;
            var storageEntry = new StorageModActionList(storage, this);
            Storages.Add(storageEntry);
            return storageEntry;
        }

        internal void RemoveStorage(IModelStorage storage)
        {
            var storageEntry = Storages.OfType<StorageModActionList>().SingleOrDefault(s => s.Storage == storage);
            if (storageEntry != null)
                Storages.Remove(storageEntry);
        }

        internal void RemoveMod(IModelStorageMod mod)
        {
            FindStorageByMod(mod)?.RemoveMod(mod);
        }

        public void Select(ModAction action)
        {
            IsOpen = false;
            SetIsSelected(action);
            if (Equals(Selection, action)) return;
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

        public void Update()
        {
            // just purging removed storages for now.
            // TODO: ideally, this should rebuild the entire list, to keep it functional / state-less
            foreach (var storageModActionList in Storages.OfType<StorageModActionList>().ToList())
            {
                if (storageModActionList.Storage.IsDeleted)
                    Storages.Remove(storageModActionList);
            }
        }
    }

    public interface IActionListEntry
    {
    }

    public class StorageModActionList : ObservableBase, IActionListEntry
    {
        internal IModelStorage Storage { get; }
        private readonly ModActionTree _parent;
        private bool _isShown;

        public string Name => Storage.Name;

        public bool IsShown
        {
            get => _isShown;
            private set
            {
                if (_isShown == value) return;
                _isShown = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<SelectableModAction> Mods { get; } = new();

        internal StorageModActionList(IModelStorage storage, ModActionTree parent)
        {
            _parent = parent;
            Storage = storage;
            if (!storage.CanWrite) return;
            Mods.Add(new SelectableModAction(new SelectStorage(storage), _parent, false));
            IsShown = true;
        }

        public void UpdateMod(SelectMod mod)
        {
            var index = FindIndex(mod.StorageMod);

            if (index == -1)
            {
                Mods.Insert(0, new SelectableModAction(mod, _parent, false));
                IsShown = true;
                return;
            }

            Mods[index] = new SelectableModAction(mod, _parent, Mods[index].IsSelected);
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
            if (!Mods.Any()) IsShown = false;
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
