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
        private readonly IActionQueue _dispatcher;
        public string Name { get; }

        public YesNoInteractionRequest UpdatePrepared { get; }

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

        internal Repository(IModelRepository repository, IModelStructure structure, IActionQueue dispatcher)
        {
            _repository = repository;
            _dispatcher = dispatcher;
            Update = new DelegateCommand(DoUpdate);
            CalculatedState = repository.CalculatedState;
            repository.CalculatedStateChanged += () =>
            {
                CalculatedState = repository.CalculatedState;
            };
            Name = repository.ToString();
            repository.ModAdded += mod => Mods.Add(new RepositoryMod(mod, structure));
            UpdatePrepared = new YesNoInteractionRequest();
        }

        private bool CanUpdate()
        {
            return CalculatedState.State == CalculatedRepositoryStateEnum.NeedsDownload ||
                   CalculatedState.State == CalculatedRepositoryStateEnum.NeedsUpdate;
        }

        private void DoUpdate()
        {
            _repository.DoUpdate(((action) =>
            {
                var text = "someBytes to download. Proceed?"; // TODO
                var context = new YesNoPopupContext(text, "Proceed with Update?");
                _dispatcher.EnQueueAction(() =>
                {
                    UpdatePrepared.Raise(context, action);
                });
            }));
        }

        public DelegateCommand Update { get; }
    }
}
