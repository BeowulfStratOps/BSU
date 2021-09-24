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
        private readonly IViewModelService _viewModelService;
        public string Name { get; }

        public FileSyncProgress UpdateProgress { get; } = new();

        public InteractionRequest<MsgPopupContext, bool> UpdatePrepared { get; } = new();
        public InteractionRequest<MsgPopupContext, bool> UpdateSetup { get; } = new();
        public InteractionRequest<MsgPopupContext, object> UpdateFinished { get; } = new();

        private CalculatedRepositoryState _calculatedState = new CalculatedRepositoryState(CalculatedRepositoryStateEnum.Loading, false);

        public CalculatedRepositoryState CalculatedState
        {
            get => _calculatedState;
            private set
            {
                if (_calculatedState == value) return;
                _calculatedState = value;
                OnPropertyChanged();
                UpdateButtonStates();
            }
        }

        private void UpdateButtonStates()
        {
            Update.State = GetUpdateActionState();
            Play.State = GetPlayActionState();
        }

        public ObservableCollection<RepositoryMod> Mods { get; } = new();

        public StateCommand Update { get; }

        public DelegateCommand Delete { get; }
        public DelegateCommand Details { get; }
        public StateCommand Play { get; }


        public DelegateCommand Back { get; }

        public DelegateCommand ShowStorage { get; }

        public InteractionRequest<MsgPopupContext, bool?> DeleteInteraction { get; } = new();
        public Guid Identifier { get; }

        private string _title = "Loading...";
        public string Title
        {
            get => _title;
            set
            {
                if (_title == value) return;
                _title = value;
                OnPropertyChanged();
            }
        }

        private string _serverUrl = "Loading...";
        public string ServerUrl
        {
            get => _serverUrl;
            set
            {
                if (_serverUrl == value) return;
                _serverUrl = value;
                OnPropertyChanged();
            }
        }

        internal Repository(IModelRepository repository, IModel model, IViewModelService viewModelService)
        {
            _repository = repository;
            _model = model;
            _viewModelService = viewModelService;
            Identifier = repository.Identifier;
            Delete = new DelegateCommand(DoDelete);
            Update = new StateCommand(DoUpdate);
            Back = new DelegateCommand(viewModelService.NavigateBack);
            ShowStorage = new DelegateCommand(viewModelService.NavigateToStorages); // TODO: select specific storage or smth?
            Details = new DelegateCommand(() => viewModelService.NavigateToRepository(this));
            Play = new StateCommand(() => throw new NotImplementedException());
            UpdateButtonStates();
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

        private CommandState GetUpdateActionState()
        {
            switch (CalculatedState.State)
            {
                case CalculatedRepositoryStateEnum.NeedsUpdate:
                case CalculatedRepositoryStateEnum.NeedsDownload:
                    return CommandState.Enabled;
                case CalculatedRepositoryStateEnum.Ready:
                case CalculatedRepositoryStateEnum.RequiresUserIntervention:
                case CalculatedRepositoryStateEnum.InProgress:
                    return CommandState.Disabled;
                case CalculatedRepositoryStateEnum.Loading:
                    return CommandState.Loading;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private CommandState GetPlayActionState()
        {
            switch (CalculatedState.State)
            {
                case CalculatedRepositoryStateEnum.NeedsUpdate:
                case CalculatedRepositoryStateEnum.NeedsDownload:
                    return CommandState.Warning;
                case CalculatedRepositoryStateEnum.Ready:
                    return CommandState.Primary;
                case CalculatedRepositoryStateEnum.RequiresUserIntervention:
                case CalculatedRepositoryStateEnum.InProgress:
                    return CommandState.Disabled;
                case CalculatedRepositoryStateEnum.Loading:
                    return CommandState.Enabled;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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

            await _viewModelService.Update();

            var updatedText = $"{updateStats.SucceededCount} Mods updated. {updateStats.FailedCount} Mods failed.";
            var updatedContext = new MsgPopupContext(updatedText, "Update Finished");
            await UpdateFinished.Raise(updatedContext);
        }

        public async Task Load()
        {
            (Title, ServerUrl) = await _repository.GetServerInfo(CancellationToken.None);
            var mods = await _repository.GetMods();
            foreach (var mod in mods)
            {
                Mods.Add(new RepositoryMod(mod, _model, _viewModelService));
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
