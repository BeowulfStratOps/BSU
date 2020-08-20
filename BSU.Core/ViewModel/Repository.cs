using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BSU.Core.Annotations;
using BSU.Core.Model;

namespace BSU.Core.ViewModel
{
    public class Repository : ViewModelClass
    {
        private readonly IModelRepository _repository;
        public string Name { get; }

        private CalculatedRepositoryState _calculatedState;

        public CalculatedRepositoryState CalculatedState
        {
            get => _calculatedState;
            private set
            {
                if (_calculatedState == value) return;
                _calculatedState = value;
                Update.SetCanExecute(CanUpdate()); // TODO: should this be a behaviour?
                OnPropertyChanged();
            }
        }

        public ObservableCollection<RepositoryMod> Mods { get; } = new ObservableCollection<RepositoryMod>();

        internal Repository(IModelRepository repository, ViewModel viewModel, IModelStructure structure)
        {
            _repository = repository;
            Update = new DelegateCommand(DoUpdate);
            CalculatedState = repository.CalculatedState;
            repository.CalculatedStateChanged += () =>
            {
                CalculatedState = repository.CalculatedState;
            };
            Name = repository.ToString();
            repository.ModAdded += mod => Mods.Add(new RepositoryMod(mod, structure));
        }

        private bool CanUpdate()
        {
            return CalculatedState.State == CalculatedRepositoryStateEnum.NeedsDownload ||
                   CalculatedState.State == CalculatedRepositoryStateEnum.NeedsUpdate;
        }

        private void DoUpdate()
        {
            _repository.DoUpdate();
        }
        
        public DelegateCommand Update { get; }
    }
}
