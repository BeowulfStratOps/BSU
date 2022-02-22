using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Events;
using BSU.Core.Ioc;
using BSU.Core.Launch;
using BSU.Core.Model;
using BSU.Core.Services;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class Repository : ObservableBase
    {
        internal readonly IModelRepository ModelRepository;
        private readonly IModel _model;
        private readonly IViewModelService _viewModelService;
        public string Name { get; }

        public FileSyncProgress UpdateProgress { get; } = new();

        private CalculatedRepositoryStateEnum _calculatedState = CalculatedRepositoryStateEnum.Loading;

        private CancellationTokenSource? _cts;

        public CalculatedRepositoryStateEnum CalculatedState
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
            // TODO: hide play button when using arma launcher

            switch (CalculatedState)
            {
                case CalculatedRepositoryStateEnum.NeedsSync:
                    Details.SetCanExecute(true);

                    Update.SetCanExecute(true);
                    UpdateButtonVisible = true;
                    PauseButtonVisible = false;
                    UpdateButtonColor = ColorIndication.Update;
                    UpdateCheckMarkVisible = false;

                    Pause.SetCanExecute(false);
                    Delete.SetCanExecute(true);

                    Play.SetCanExecute(true);
                    PlayButtonColor = ColorIndication.Warning;
                    break;
                case CalculatedRepositoryStateEnum.Ready:
                    Details.SetCanExecute(true);

                    Update.SetCanExecute(false);
                    UpdateButtonVisible = true;
                    PauseButtonVisible = false;
                    UpdateButtonColor = ColorIndication.Normal;
                    UpdateCheckMarkVisible = true;

                    Pause.SetCanExecute(false);
                    Delete.SetCanExecute(true);

                    Play.SetCanExecute(true);
                    PlayButtonColor = ColorIndication.Good;
                    break;
                case CalculatedRepositoryStateEnum.RequiresUserIntervention:
                    Details.SetCanExecute(true);

                    Update.SetCanExecute(false);
                    UpdateButtonVisible = true;
                    PauseButtonVisible = false;
                    UpdateButtonColor = ColorIndication.Normal;
                    UpdateCheckMarkVisible = false;

                    Pause.SetCanExecute(false);
                    Delete.SetCanExecute(true);

                    Play.SetCanExecute(false);
                    PlayButtonColor = ColorIndication.Normal;
                    break;
                case CalculatedRepositoryStateEnum.Syncing:
                    Details.SetCanExecute(true);

                    Update.SetCanExecute(false);
                    UpdateButtonVisible = false;
                    PauseButtonVisible = true;
                    UpdateButtonColor = ColorIndication.Normal;
                    UpdateCheckMarkVisible = false;

                    Pause.SetCanExecute(_cts is { IsCancellationRequested: false });
                    Delete.SetCanExecute(false);

                    Play.SetCanExecute(false);
                    PlayButtonColor = ColorIndication.Normal;
                    break;
                case CalculatedRepositoryStateEnum.Loading:
                    Details.SetCanExecute(true);

                    Update.SetCanExecute(false);
                    UpdateButtonVisible = true;
                    PauseButtonVisible = false;
                    UpdateButtonColor = ColorIndication.Normal;
                    UpdateCheckMarkVisible = false;

                    Pause.SetCanExecute(false);
                    Delete.SetCanExecute(false);

                    Play.SetCanExecute(true);
                    PlayButtonColor = ColorIndication.Warning;
                    break;
                case CalculatedRepositoryStateEnum.ReadyPartial:
                    Details.SetCanExecute(true);

                    Update.SetCanExecute(false);
                    UpdateButtonVisible = true;
                    PauseButtonVisible = false;
                    UpdateButtonColor = ColorIndication.Normal;
                    UpdateCheckMarkVisible = true;

                    Pause.SetCanExecute(false);
                    Delete.SetCanExecute(true);

                    Play.SetCanExecute(true);
                    PlayButtonColor = ColorIndication.Warning;
                    break;
                case CalculatedRepositoryStateEnum.Error:
                    Details.SetCanExecute(false);

                    Update.SetCanExecute(false);
                    UpdateButtonVisible = true;
                    PauseButtonVisible = false;
                    UpdateButtonColor = ColorIndication.Normal;
                    UpdateCheckMarkVisible = false;

                    Pause.SetCanExecute(false);
                    Delete.SetCanExecute(true);

                    Play.SetCanExecute(false);
                    PlayButtonColor = ColorIndication.Normal;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (ModelRepository.State == LoadingState.Loaded)
            {
                var selections = ModelRepository.GetMods().Select(mod => mod.GetCurrentSelection()).ToList();

                ChooseDownloadLocation.SetCanExecute(selections.Any(s => s is ModSelectionDownload or ModSelectionNone));
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


        private GameLaunchHandle? _runningProcessHandle;

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning == value) return;
                _isRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NotIsRunning));
            }
        }

        public bool NotIsRunning => !IsRunning;

        internal Repository(IModelRepository modelRepository, IServiceProvider serviceProvider)
        {
            ModelRepository = modelRepository;
            _model = serviceProvider.Get<IModel>();
            _viewModelService = serviceProvider.Get<IViewModelService>();
            _interactionService = serviceProvider.Get<IInteractionService>();
            var asyncVoidExecutor = serviceProvider.Get<IAsyncVoidExecutor>();
            Delete = new DelegateCommand(DoDelete, false);
            Update = new DelegateCommand(() => asyncVoidExecutor.Execute(DoUpdate));
            Back = new DelegateCommand(_viewModelService.NavigateBack);
            ShowStorage = new DelegateCommand(_viewModelService.NavigateToStorages); // TODO: select specific storage or smth?
            Details = new DelegateCommand(() => _viewModelService.NavigateToRepository(this));
            Play = new DelegateCommand(DoPlay);
            StopPlaying = new DelegateCommand(() => asyncVoidExecutor.Execute(DoStopPlaying));
            Pause = new DelegateCommand(DoPause, false);
            ChooseDownloadLocation = new DelegateCommand(DoChooseDownloadLocation, false);
            modelRepository.StateChanged += _ => OnStateChanged();
            Name = modelRepository.Name;
            var eventManager = serviceProvider.Get<IEventManager>();
            eventManager.Subscribe<CalculatedStateChangedEvent>(UpdateState);
            _stateService = serviceProvider.Get<IRepositoryStateService>();
            _services = serviceProvider;
        }

        private void UpdateState(CalculatedStateChangedEvent evt)
        {
            if (evt.Repository != ModelRepository) return;
            CalculatedState = _stateService.GetStateFor(ModelRepository);
            UpdateButtonStates();
        }

        private void OnStateChanged()
        {
            if (ModelRepository.State == LoadingState.Error)
            {
                ServerUrl = "";
                return;
            }

            ServerUrl = ModelRepository.GetServerInfo().Url;
            var mods = ModelRepository.GetMods();
            foreach (var mod in mods.OrderBy(m => m.Identifier))
            {
                Mods.Add(new RepositoryMod(mod, _services));
            }
        }

        private void DoChooseDownloadLocation()
        {
            var vm = new SelectRepositoryStorage(ModelRepository, _services, false);
            _interactionService.SelectRepositoryStorage(vm);
        }

        private void DoPlay()
        {
            if (_runningProcessHandle != null) throw new InvalidOperationException();

            var warningMessage = CalculatedState switch
            {
                CalculatedRepositoryStateEnum.NeedsSync => "Your mods are not up to date.",
                CalculatedRepositoryStateEnum.Ready => null,
                CalculatedRepositoryStateEnum.Loading => "The sync utility is still checking your mods.",
                CalculatedRepositoryStateEnum.ReadyPartial => "You have disabled some mods.",
                _ => throw new ArgumentOutOfRangeException()
            };

            if (warningMessage != null)
            {
                warningMessage += " Are your sure you want to launch the game?";
                var goAhead = _interactionService.YesNoPopup(warningMessage, "Launch Game");
                if (!goAhead) return;
            }

            var launchResult = ModelRepository.Launch();

            if (launchResult == null) return; // no process tracking, aka Arma launcher

            if (launchResult.Succeeded)
            {
                _runningProcessHandle = launchResult.GetHandle();
                _runningProcessHandle.Exited += () =>
                {
                    _runningProcessHandle = null;
                    IsRunning = false;
                };
                IsRunning = true;
            }
            else
            {
                _interactionService.MessagePopup(launchResult.GetFailedReason(), "Failed to launch");
            }
        }

        private async Task DoStopPlaying()
        {
            if (_runningProcessHandle == null) throw new InvalidOperationException();
            await _runningProcessHandle.Stop();
        }

        private void DoPause()
        {
            if (_cts == null) throw new InvalidOperationException();
            _cts.Cancel();
            UpdateButtonStates();
        }

        private void DoDelete(object? objOnDetailsPage)
        {
            // TODO: this doesn't look like it belongs here
            var text = $@"Removing repository {Name}. Do you want to remove mods used by this repository?

Yes - Delete mods if they are not in use by any other repository
No - Keep local mods
Cancel - Do not remove this repository";

            var removeData = _interactionService.YesNoCancelPopup(text, "Remove Repository");
            if (removeData == null) return;

            if (removeData == true)
            {
                _interactionService.MessagePopup("Removing mods is not supported yet.", "Not supported");
                return;
            }

            _model.DeleteRepository(ModelRepository, (bool)removeData);

            if ((bool) objOnDetailsPage!) _viewModelService.NavigateBack();
        }

        public async Task DoUpdate()
        {
            _cts = new CancellationTokenSource();
            foreach (var mod in Mods)
            {
                mod.Actions.Open.SetCanExecute(false);
            }

            try
            {
                var startTime = DateTime.Now;
                var updateTasks = Mods.Select(m => m.StartUpdate(_cts.Token)).ToList();
                await Task.WhenAll(updateTasks);
                var updates = updateTasks.Where(r => r.Result != null).Select(t => t.Result!).ToList();

                var updateStats = await RepositoryUpdate.Update(updates,  UpdateProgress.Progress);

                if (_cts.IsCancellationRequested) return;

                if (!updateStats.Failed.Any() && !updateStats.FailedSharingViolation.Any())
                {
                    _interactionService.MessagePopup($"Update completed in {(DateTime.Now-startTime):hh\\:mm\\:ss}.", "Update Complete");
                    return;
                }

                var updatedText = $"{updateStats.SucceededCount} Mods updated.";
                if (updateStats.FailedSharingViolation.Any())
                {
                    updatedText += "\nFailed due to being open in another process: " + string.Join(", ",
                        updateStats.FailedSharingViolation.Select(s => $"{s.ParentStorage.Name}/{s.Identifier}"));
                }
                if (updateStats.Failed.Any())
                {
                    updatedText += "\nFailed due to unknown reason (see logs): " + string.Join(", ",
                        updateStats.Failed.Select(s => $"{s.ParentStorage.Name}/{s.Identifier}"));
                }
                _interactionService.MessagePopup(updatedText, "Update finished");
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                foreach (var mod in Mods)
                {
                    mod.Actions.Open.SetCanExecute(true);
                }
            }
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

        private ColorIndication _playButtonColor = ColorIndication.Good;
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

        private bool _updateButtonVisible = true;
        private readonly IInteractionService _interactionService;
        private readonly IServiceProvider _services;
        private readonly IRepositoryStateService _stateService;

        public bool UpdateButtonVisible
        {
            get => _updateButtonVisible;
            set
            {
                if (_updateButtonVisible == value) return;
                _updateButtonVisible = value;
                OnPropertyChanged();
            }
        }

        public DelegateCommand ChooseDownloadLocation { get; }

        public DelegateCommand StopPlaying { get; }

        private bool _pauseButtonVisible;
        public bool PauseButtonVisible
        {
            get => _pauseButtonVisible;
            set
            {
                if (_pauseButtonVisible == value) return;
                _pauseButtonVisible = value;
                OnPropertyChanged();
            }
        }

        private bool _updateCheckMarkVisible = true;
        public bool UpdateCheckMarkVisible
        {
            get => _updateCheckMarkVisible;
            set => SetProperty(ref _updateCheckMarkVisible, value);
        }

        #endregion
    }
}
