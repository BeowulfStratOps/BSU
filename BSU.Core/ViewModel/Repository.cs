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
        private readonly Func<Task> _updateViewModel;
        public string Name { get; }

        public FileSyncProgress UpdateProgress { get; } = new();

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

        internal Repository(IModelRepository repository, IModel model, Func<Task> updateViewModel)
        {
            _repository = repository;
            _model = model;
            _updateViewModel = updateViewModel;
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
            var updateTasks = Mods.Select(m => m.StartUpdate(CancellationToken.None)).ToList();
            await Task.WhenAll(updateTasks);
            var updates = updateTasks.Select(t => t.Result).Where(r => r.update != null).ToList();

            var progress = UpdateProgress.Progress;

            var update = new RepositoryUpdate(updates, progress);

            var prepareStats = await update.Prepare(CancellationToken.None);
            var bytes = 0;
            var preparedText = $"{bytes} Bytes from {prepareStats.SucceededCount} mods to download. {prepareStats.FailedCount} mods failed. Proceed?";
            var preparedContext = new MsgPopupContext(preparedText, "Update Prepared");
            if (!await UpdatePrepared.Raise(preparedContext))
            {
                throw new NotImplementedException();
                // prepared.Abort();
                return;
            }

            var updateStats = await update.Update(CancellationToken.None);

            await _updateViewModel();

            var updatedText = $"{updateStats.SucceededCount} Mods updated. {updateStats.FailedCount} Mods failed.";
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
                Mods.Add(new RepositoryMod(mod, _model, _updateViewModel));
            }
            await Task.WhenAll(Mods.Select(m => m.Load()));
        }

        public async Task UpdateMods()
        {
            await Task.WhenAll(Mods.Select(m => m.Update()));
            CalculatedState = await _repository.GetState(CancellationToken.None);
        }
    }
}
