using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BSU.Core.Annotations;
using BSU.Core.Model;

namespace BSU.Core.ViewModel
{
    public class RepositoryMod : INotifyPropertyChanged
    {
        private readonly IModelRepositoryMod _mod;
        
        
        
        public string Name { get; }
        public string DisplayName { private set; get; }

        public bool IsLoading { private set; get; }


        public ObservableCollection<ModAction> Actions { get; } = new ObservableCollection<ModAction>();

        private ModAction _selection;
        public ModAction Selection
        {
            get => _selection;
            set
            {
                if (_selection == value) return;
                _selection = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal RepositoryMod(IModelRepositoryMod mod, IModelStructure structure)
        {
            IsLoading = mod.GetState().IsLoading;
            _mod = mod;
            Name = mod.ToString();
            Actions.Add(new ModAction(new RepositoryModActionSelection(), _mod.Actions));
            mod.ActionAdded += AddAction;
            foreach (var target in mod.Actions.Keys)
            {
                AddAction(target);
            }
            
            Selection = new ModAction(mod.Selection, mod.Actions);
            mod.SelectionChanged += () =>
            {
                Selection = new ModAction(mod.Selection, mod.Actions);
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
            SelectionChanged = new TestCommand(Test);
        }

        private void AddAction(IModelStorageMod storageMod)
        {
            Actions.Add(new ModAction(new RepositoryModActionSelection(storageMod), _mod.Actions));
        }
        
        internal void AddStorage(IModelStorage storage)
        {
            if (!storage.CanWrite) return;
            Actions.Add(new ModAction(new RepositoryModActionSelection(storage), _mod.Actions));
        }

        private void Test()
        {
            _mod.Selection = _selection?.Selection;
        }

        public TestCommand SelectionChanged { get; }
    }

    public class TestCommand : ICommand
    {
        private readonly Action _action;

        public TestCommand(Action action)
        {
            _action = action;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _action();
        }

        public event EventHandler CanExecuteChanged;
    }
}
