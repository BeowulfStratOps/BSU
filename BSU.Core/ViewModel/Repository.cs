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

        private CalculatedRepositoryState _calculatedState = new CalculatedRepositoryState(CalculatedRepositoryStateEnum.Loading);

        private CancellationTokenSource _cts;

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
            switch (CalculatedState.State)
            {
                case CalculatedRepositoryStateEnum.NeedsSync:
                    Update.SetCanExecute(true);
                    UpdateLoading = false;
                    UpdateButtonColor = ColorIndication.Primary;

                    Pause.SetCanExecute(false);
                    Delete.SetCanExecute(true);

                    Play.SetCanExecute(true);
                    PlayButtonColor = ColorIndication.Warning;
                    break;
                case CalculatedRepositoryStateEnum.Ready:
                    Update.SetCanExecute(false);
                    UpdateLoading = false;
                    UpdateButtonColor = ColorIndication.Normal;

                    Pause.SetCanExecute(false);
                    Delete.SetCanExecute(true);

                    Play.SetCanExecute(true);
                    PlayButtonColor = ColorIndication.Primary;
                    break;
                case CalculatedRepositoryStateEnum.RequiresUserIntervention:
                    Update.SetCanExecute(false);
                    UpdateLoading = false;
                    UpdateButtonColor = ColorIndication.Normal;

                    Pause.SetCanExecute(false);
                    Delete.SetCanExecute(true);

                    Play.SetCanExecute(false);
                    PlayButtonColor = ColorIndication.Normal;
                    break;
                case CalculatedRepositoryStateEnum.Syncing:
                    Update.SetCanExecute(false);
                    UpdateLoading = false;
                    UpdateButtonColor = ColorIndication.Normal;

                    Pause.SetCanExecute(true);
                    Delete.SetCanExecute(false);

                    Play.SetCanExecute(false);
                    PlayButtonColor = ColorIndication.Normal;
                    break;
                case CalculatedRepositoryStateEnum.Loading:
                    Update.SetCanExecute(false);
                    UpdateLoading = true;
                    UpdateButtonColor = ColorIndication.Normal;

                    Pause.SetCanExecute(false);
                    Delete.SetCanExecute(false);

                    Play.SetCanExecute(true);
                    PlayButtonColor = ColorIndication.Warning;
                    break;
                case CalculatedRepositoryStateEnum.ReadyPartial:
                    Update.SetCanExecute(false);
                    UpdateLoading = false;
                    UpdateButtonColor = ColorIndication.Normal;

                    Pause.SetCanExecute(false);
                    Delete.SetCanExecute(true);

                    Play.SetCanExecute(true);
                    PlayButtonColor = ColorIndication.Warning;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public ObservableCollection<RepositoryMod> Mods { get; } = new();

        public DelegateCommand Update { get; }
        public DelegateCommand Pause { get; }

        public DelegateCommand Delete { get; }
        public DelegateCommand Details { get; }
        public DelegateCommand Play { get; }


        public DelegateCommand Back { get; }

        public DelegateCommand ShowStorage { get; }
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
            Delete = new DelegateCommand(DoDelete, false);
            Update = new DelegateCommand(DoUpdate);
            Back = new DelegateCommand(viewModelService.NavigateBack);
            ShowStorage = new DelegateCommand(viewModelService.NavigateToStorages); // TODO: select specific storage or smth?
            Details = new DelegateCommand(() => viewModelService.NavigateToRepository(this));
            Play = new DelegateCommand(() => PlayButtonColor = ColorIndication.Primary);
            Pause = new DelegateCommand(DoPause, false);
            UpdateButtonStates();
            Name = repository.Name;
        }

        private void DoPause()
        {
            _cts.Cancel();
        }

        private void DoDelete()
        {
            // TODO: this doesn't look like it belongs here
            var text = $@"Removing repository {Name}. Do you want to remove mods used by this repository?

Yes - Delete mods if they are not in use by any other repository
No - Keep local mods
Cancel - Do not remove this repository";

            var removeData = _viewModelService.InteractionService.YesNoCancelPopup(text, "Remove Repository");
            if (removeData != null) // not canceled
                _model.DeleteRepository(_repository, (bool)removeData);
        }

        private async Task DoUpdate()
        {
            _cts = new CancellationTokenSource();
            foreach (var mod in Mods)
            {
                mod.CanChangeSelection = false;
            }

            try
            {
                var updateTasks = Mods.Select(m => m.StartUpdate(CancellationToken.None)).ToList();
                await Task.WhenAll(updateTasks);
                var updates = updateTasks.Select(t => t.Result).Where(r => r.update != null).ToList();

                var progress = UpdateProgress.Progress;

                var update = new RepositoryUpdate(updates, progress);

                await update.Prepare(_cts.Token);
                var updateStats = await update.Update(_cts.Token);

                await _viewModelService.Update();

                if (updateStats.FailedCount == 0)
                {
                    _viewModelService.InteractionService.MessagePopup("Update Complete", "Update Complete");
                    return;
                }

                var updatedText = $"{updateStats.SucceededCount} Mods updated. {updateStats.FailedCount} Mods failed.";
                _viewModelService.InteractionService.MessagePopup(updatedText, "Update finished");
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                foreach (var mod in Mods)
                {
                    mod.CanChangeSelection = true;
                }
                await _viewModelService.Update();
            }
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

        #region UI Properties

        private ColorIndication _updateButtonColor = ColorIndication.Warning;
        public ColorIndication UpdateButtonColor
        {
            get => _updateButtonColor;
            set
            {
                if (_updateButtonColor == value) return;
                _updateButtonColor = value;
                OnPropertyChanged();
            }
        }

        private ColorIndication _playButtonColor = ColorIndication.Primary;
        public ColorIndication PlayButtonColor
        {
            get => _playButtonColor;
            set
            {
                if (_playButtonColor == value) return;
                _playButtonColor = value;
                OnPropertyChanged();
            }
        }

        private bool _updateLoading;
        public bool UpdateLoading
        {
            get => _updateLoading;
            set
            {
                if (_updateLoading == value) return;
                _updateLoading = value;
                OnPropertyChanged();
            }
        }

        #endregion
    }
}
