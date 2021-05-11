using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.Core.Model.Utility;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class Repository : ObservableBase
    {
        private readonly IModelRepository _repository;
        private readonly IModel _model;
        public string Name { get; }

        public InteractionRequest<MsgPopupContext, bool> UpdatePrepared { get; } = new InteractionRequest<MsgPopupContext, bool>();
        public InteractionRequest<MsgPopupContext, bool> UpdateSetup { get; } = new InteractionRequest<MsgPopupContext, bool>();
        public InteractionRequest<MsgPopupContext, object> UpdateFinished { get; } = new InteractionRequest<MsgPopupContext, object>();

        private CalculatedRepositoryState _calculatedState;
        private IProgressProvider _updateProgress;
        private readonly IModelStructure _modelStructure;

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

        internal Repository(IModelRepository repository, IModel model, IModelStructure modelStructure)
        {
            _repository = repository;
            _model = model;
            _modelStructure = modelStructure;
            Identifier = repository.Identifier;
            Delete = new DelegateCommand(DoDelete);
            Update = new DelegateCommand(DoUpdate);
            CalculatedState = new CalculatedRepositoryState(CalculatedRepositoryStateEnum.Loading, false);
            repository.CalculatedStateChanged += state =>
            {
                CalculatedState = state;
            };
            Name = repository.Name;
            repository.ModAdded += mod => Mods.Add(new RepositoryMod(mod, model, _modelStructure));
        }

        private async Task DoDelete()
        {
            // TODO: this doesn't look like it belongs here
            var text = $@"Removing repository {Name}. Do you want to remove mods used by this repository?

Yes - Delete mods if they are not in use by any other repository
No - Keep local mods
Cancel - Do not remove this repository";

            var context = new MsgPopupContext(text, "Remove Repository");
            var removeData = await DeleteInteraction.Raise(context);
            if (removeData != null) // not canceled
                _model.DeleteRepository(_repository, (bool)removeData);
        }

        private bool CanUpdate()
        {
            return CalculatedState.State == CalculatedRepositoryStateEnum.NeedsDownload ||
                   CalculatedState.State == CalculatedRepositoryStateEnum.NeedsUpdate;
        }

        private async Task DoUpdate()
        {
            var update = _repository.DoUpdate(); // TODO: use it with using, to make sure it's cleaned up
            var created = await update.Create();
            if (created.Failed.Any())
            {
                var folders = string.Join(", ", created.Failed.Select(f => "smth"));
                var createdText = $"There were errors while creating mod folders: {folders}. Proceed?";
                var createdContext = new MsgPopupContext(createdText, "Proceed with Update?");
                if (!await UpdateSetup.Raise(createdContext))
                {
                    update.Abort();
                    return;
                }
            }
            var prepared = await update.Prepare();
            var bytes = prepared.Succeeded.Sum(s => s.GetPrepStats());
            var preparedText = $"{bytes} Bytes from {prepared.Succeeded.Count} mods to download. {prepared.Failed.Count} mods failed. Proceed?";
            var preparedContext = new MsgPopupContext(preparedText, "Update Prepared");
            if (!await UpdatePrepared.Raise(preparedContext))
            {
                update.Abort();
                return;
            }

            var updated = await update.Update();
            var updatedText = $"{updated.Succeeded.Count} Mods updated. {updated.Failed.Count} Mods failed.";
            var updatedContext = new MsgPopupContext(updatedText, "Update Finished");
            await UpdateFinished.Raise(updatedContext);
        }

        public DelegateCommand Update { get; }

        public DelegateCommand Delete { get; }
        public InteractionRequest<MsgPopupContext, bool?> DeleteInteraction { get; } = new InteractionRequest<MsgPopupContext, bool?>();
        public Guid Identifier { get; }

        public IProgressProvider UpdateProgress
        {
            get => _updateProgress;
            private set
            {
                if (_updateProgress == value) return;
                _updateProgress = value;
                OnPropertyChanged();
            }
        }
    }
}
