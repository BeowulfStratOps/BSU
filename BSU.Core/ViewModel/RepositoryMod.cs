using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BSU.Core.Annotations;
using BSU.Core.Model;

namespace BSU.Core.ViewModel
{
    public class RepositoryMod : ViewModelClass
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

        //public override event PropertyChangedEventHandler PropertyChanged;

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
            SelectionChanged = new DelegateCommand(Test);
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

        public DelegateCommand SelectionChanged { get; }
    }
}
