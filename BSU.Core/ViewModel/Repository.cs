using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BSU.Core.Model;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class Repository : ObservableBase
    {
        private readonly IModelRepository _repository;
        private readonly IModel _model;
        private readonly IActionQueue _dispatcher;
        public string Name { get; }

        public InteractionRequest<MsgPopupContext, bool> UpdatePrepared { get; } = new InteractionRequest<MsgPopupContext, bool>();
        public InteractionRequest<MsgPopupContext, bool> UpdateSetup { get; } = new InteractionRequest<MsgPopupContext, bool>();
        public InteractionRequest<MsgPopupContext, object> UpdateFinished { get; } = new InteractionRequest<MsgPopupContext, object>();

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

        internal Repository(IModelRepository repository, IModel model, IActionQueue dispatcher)
        {
            _repository = repository;
            _model = model;
            _dispatcher = dispatcher;
            Identifier = repository.Identifier;
            Delete = new DelegateCommand(DoDelete);
            Update = new DelegateCommand(DoUpdate);
            CalculatedState = repository.CalculatedState;
            repository.CalculatedStateChanged += () =>
            {
                CalculatedState = repository.CalculatedState;
            };
            Name = repository.Name;
            repository.ModAdded += mod => Mods.Add(new RepositoryMod(mod, model));
        }

        private void DoDelete()
        {
            // TODO: this doesn't look like it belongs here
            var text = $@"Removing repository {Name}. Do you want to remove mods used by this repository?

Yes - Delete mods if they are not in use by any other repository
No - Keep local mods
Cancel - Do not remove this repository";
            
            var context = new MsgPopupContext(text, "Remove Repository");
            DeleteInteraction.Raise(context, b =>
            {
                if (b == null) return;
                _model.DeleteRepository(_repository, (bool) b);
            });
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

        private void Finished(StageCallbackArgs args)
        {
            var text = $"{args.Succeeded.Count} Mods updated. {args.Failed.Count} Mods failed.";
            var context = new MsgPopupContext(text, "Update Finished");
            UpdateFinished.Raise(context, o => { });
        }

        private void Prepared(StageCallbackArgs args, Action<bool> proceed)
        {
            var bytes = args.Succeeded.Sum(s => s.GetPrepStats());
            var text = $"{bytes} Bytes from {args.Succeeded.Count} mods to download. {args.Failed.Count} mods failed. Proceed?";
            var context = new MsgPopupContext(text, "Update Prepared");
            UpdatePrepared.Raise(context, proceed);
        }

        private void SetUp(StageCallbackArgs args, Action<bool> proceed)
        {
            if (!args.Failed.Any())
            {
                proceed(true);
                return;
            }

            //var folders = string.Join(", ", args.Failed.Select(f => f.Identifier));
            var folders = "TODO";
            var text = $"There were errors while creating mod folders: {folders}. Proceed?";
            var context = new MsgPopupContext(text, "Proceed with Update?");
            UpdateSetup.Raise(context, proceed);
        }

        public DelegateCommand Update { get; }
        
        public DelegateCommand Delete { get; }
        public InteractionRequest<MsgPopupContext, bool?> DeleteInteraction { get; } = new InteractionRequest<MsgPopupContext, bool?>();
        public Guid Identifier { get; }
    }
}
