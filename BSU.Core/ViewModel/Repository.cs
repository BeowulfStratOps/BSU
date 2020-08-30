using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BSU.Core.Model;

namespace BSU.Core.ViewModel
{
    public class Repository : ViewModelClass
    {
        private readonly IModelRepository _repository;
        private readonly IActionQueue _dispatcher;
        public string Name { get; }

        public YesNoInteractionRequest UpdatePrepared { get; }
        public YesNoInteractionRequest UpdateSetup { get; }
        public MsgInteractionRequest UpdateFinished { get; }

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

            UpdateSetup = new YesNoInteractionRequest();
            UpdatePrepared = new YesNoInteractionRequest();
            UpdateFinished = new MsgInteractionRequest();
        }

        private bool CanUpdate()
        {
            return CalculatedState.State == CalculatedRepositoryStateEnum.NeedsDownload ||
                   CalculatedState.State == CalculatedRepositoryStateEnum.NeedsUpdate;
        }

        private void DoUpdate()
        {
            _repository.DoUpdate(SetUp, Prepared, Finished);
        }

        private void Finished(FinishedArgs args)
        {
            var text = $"{args.Succeeded.Count} Mods updated. {args.Failed.Count} Mods failed.";
            var context = new MsgPopupContext(text, "Update Finished");
            _dispatcher.EnQueueAction(() =>
            {
                UpdateFinished.Raise(context);
            });
        }

        private void Prepared(PreparedArgs args, Action<bool> proceed)
        {
            var bytes = args.Succeeded.Sum(s => s.GetPrepStats());
            var text = $"{bytes} Bytes from {args.Succeeded.Count} mods to download. {args.Failed.Count} mods failed. Proceed?";
            var context = new YesNoPopupContext(text, "Update Prepared");
            _dispatcher.EnQueueAction(() =>
            {
                UpdatePrepared.Raise(context, proceed);
            });
        }

        private void SetUp(SetUpArgs args, Action<bool> proceed)
        {
            if (!args.Failed.Any())
            {
                _dispatcher.EnQueueAction(() => proceed(true));
                return;
            }

            var folders = string.Join(", ", args.Failed.Select(f => f.Identifier));
            var text = $"There were errors while creating mod folders: {folders}. Proceed?";
            var context = new YesNoPopupContext(text, "Proceed with Update?");
            _dispatcher.EnQueueAction(() =>
            {
                UpdateSetup.Raise(context, proceed);
            });
        }

        public DelegateCommand Update { get; }
    }
}
