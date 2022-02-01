﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
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
                    UpdateLoading = false;
                    UpdateButtonColor = ColorIndication.Primary;

                    Pause.SetCanExecute(false);
                    Delete.SetCanExecute(true);

                    Play.SetCanExecute(true);
                    PlayButtonColor = ColorIndication.Warning;
                    break;
                case CalculatedRepositoryStateEnum.Ready:
                    Details.SetCanExecute(true);

                    Update.SetCanExecute(false);
                    UpdateButtonVisible = true;
                    UpdateLoading = false;
                    UpdateButtonColor = ColorIndication.Normal;

                    Pause.SetCanExecute(false);
                    Delete.SetCanExecute(true);

                    Play.SetCanExecute(true);
                    PlayButtonColor = ColorIndication.Primary;
                    break;
                case CalculatedRepositoryStateEnum.RequiresUserIntervention:
                    Details.SetCanExecute(true);

                    Update.SetCanExecute(false);
                    UpdateButtonVisible = true;
                    UpdateLoading = false;
                    UpdateButtonColor = ColorIndication.Normal;

                    Pause.SetCanExecute(false);
                    Delete.SetCanExecute(true);

                    Play.SetCanExecute(false);
                    PlayButtonColor = ColorIndication.Normal;
                    break;
                case CalculatedRepositoryStateEnum.Syncing:
                    Details.SetCanExecute(true);

                    Update.SetCanExecute(false);
                    UpdateButtonVisible = false;
                    UpdateLoading = false;
                    UpdateButtonColor = ColorIndication.Normal;

                    Pause.SetCanExecute(true);
                    Delete.SetCanExecute(false);

                    Play.SetCanExecute(false);
                    PlayButtonColor = ColorIndication.Normal;
                    break;
                case CalculatedRepositoryStateEnum.Loading:
                    Details.SetCanExecute(true);

                    Update.SetCanExecute(false);
                    UpdateButtonVisible = false;
                    UpdateLoading = true;
                    UpdateButtonColor = ColorIndication.Normal;

                    Pause.SetCanExecute(false);
                    Delete.SetCanExecute(false);

                    Play.SetCanExecute(true);
                    PlayButtonColor = ColorIndication.Warning;
                    break;
                case CalculatedRepositoryStateEnum.ReadyPartial:
                    Details.SetCanExecute(true);

                    Update.SetCanExecute(false);
                    UpdateButtonVisible = true;
                    UpdateLoading = false;
                    UpdateButtonColor = ColorIndication.Normal;

                    Pause.SetCanExecute(false);
                    Delete.SetCanExecute(true);

                    Play.SetCanExecute(true);
                    PlayButtonColor = ColorIndication.Warning;
                    break;
                case CalculatedRepositoryStateEnum.Error:
                    Details.SetCanExecute(false);

                    Update.SetCanExecute(false);
                    UpdateButtonVisible = true;
                    UpdateLoading = false;
                    UpdateButtonColor = ColorIndication.Normal;

                    Pause.SetCanExecute(false);
                    Delete.SetCanExecute(true);

                    Play.SetCanExecute(false);
                    PlayButtonColor = ColorIndication.Normal;
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
                OnPropertyChanged(nameof(IsNotRunning));
            }
        }

        public bool IsNotRunning => !_isRunning;

        internal Repository(IModelRepository modelRepository, IServiceProvider serviceProvider)
        {
            ModelRepository = modelRepository;
            _model = serviceProvider.Get<IModel>();
            _viewModelService = serviceProvider.Get<IViewModelService>();
            _interactionService = serviceProvider.Get<IInteractionService>();
            Identifier = modelRepository.Identifier;
            Delete = new DelegateCommand(DoDelete, false);
            Update = new DelegateCommand(() => AsyncVoidExecutor.Execute(DoUpdate));
            Back = new DelegateCommand(_viewModelService.NavigateBack);
            ShowStorage = new DelegateCommand(_viewModelService.NavigateToStorages); // TODO: select specific storage or smth?
            Details = new DelegateCommand(() => _viewModelService.NavigateToRepository(this));
            Play = new DelegateCommand(DoPlay);
            StopPlaying = new DelegateCommand(() => AsyncVoidExecutor.Execute(DoStopPlaying));
            Pause = new DelegateCommand(DoPause, false);
            Settings = new DelegateCommand(ShowSettings);
            ChooseDownloadLocation = new DelegateCommand(DoChooseDownloadLocation);
            modelRepository.StateChanged += _ => OnStateChanged();
            Name = modelRepository.Name;
            var eventManager = serviceProvider.Get<IEventManager>();
            eventManager.Subscribe<AnythingChangedEvent>(UpdateState);
            _services = serviceProvider;
        }

        private void UpdateState(AnythingChangedEvent evt)
        {
            CalculatedState = CoreCalculation.GetRepositoryState(ModelRepository, _model.GetRepositoryMods());
            UpdateButtonStates();
        }

        private void OnStateChanged()
        {
            if (ModelRepository.State == LoadingState.Error)
            {
                ServerUrl = "";
            }

            ServerUrl = ModelRepository.GetServerInfo().Url;
            var mods = ModelRepository.GetMods();
            foreach (var mod in mods.OrderBy(m => m.Identifier))
            {
                Mods.Add(new RepositoryMod(mod, _services));
            }
        }

        private void ShowSettings()
        {
            var vm = new PresetSettings(ModelRepository.Settings, true);
            var save = _interactionService.PresetSettings(vm);
            if (save)
                ModelRepository.Settings = vm.ToLaunchSettings();
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
                var updateTasks = Mods.Select(m => m.StartUpdate(CancellationToken.None)).ToList();
                await Task.WhenAll(updateTasks);
                var updates = updateTasks.Where(r => r.Result != null).Select(t => t.Result!).ToList();

                var updateStats = await RepositoryUpdate.Update(updates,  UpdateProgress.Progress);

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
        private bool _updateButtonVisible = true;
        private readonly IInteractionService _interactionService;
        private readonly IServiceProvider _services;

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

        public DelegateCommand Settings { get; }

        public DelegateCommand ChooseDownloadLocation { get; }

        public DelegateCommand StopPlaying { get; }

        #endregion
    }
}
