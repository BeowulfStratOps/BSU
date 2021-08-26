using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model;
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

        public ObservableCollection<RepositoryMod> Mods { get; } = new();

        internal Repository(IModelRepository repository, IModel model)
        {
            _repository = repository;
            _model = model;
            Identifier = repository.Identifier;
            Delete = new DelegateCommand(DoDelete);
            Update = new DelegateCommand(DoUpdate);
            Name = repository.Name;
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
            var update = await _repository.DoUpdate(CancellationToken.None); // TODO: use it with using, to make sure it's cleaned up

            var prepared = await update.Prepare(CancellationToken.None);
            var bytes = prepared.Stats.Succeeded.Sum(s => 0);
            var preparedText = $"{bytes} Bytes from {prepared.Stats.Succeeded.Count} mods to download. {prepared.Stats.FailedCount} mods failed. Proceed?";
            var preparedContext = new MsgPopupContext(preparedText, "Update Prepared");
            if (!await UpdatePrepared.Raise(preparedContext))
            {
                // prepared.Abort();
                return;
            }

            var updated = await prepared.Update(CancellationToken.None);
            var updatedText = $"{updated.Stats.Succeeded.Count} Mods updated. {updated.Stats.FailedCount} Mods failed.";
            var updatedContext = new MsgPopupContext(updatedText, "Update Finished");
            await UpdateFinished.Raise(updatedContext);
        }

        public DelegateCommand Update { get; }

        public DelegateCommand Delete { get; }
        public InteractionRequest<MsgPopupContext, bool?> DeleteInteraction { get; } = new();
        public Guid Identifier { get; }

        public async Task Load()
        {
            var mods = await _repository.GetMods();
            foreach (var mod in mods)
            {
                Mods.Add(new RepositoryMod(mod, _model));
            }

            await Task.WhenAll(Mods.Select(m => m.Load()));
        }
    }
}
