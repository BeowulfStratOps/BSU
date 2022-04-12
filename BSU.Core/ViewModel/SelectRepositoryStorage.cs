using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BSU.Core.Events;
using BSU.Core.Ioc;
using BSU.Core.Model;
using BSU.Core.Services;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public sealed class SelectRepositoryStorage : ObservableBase, IDisposable
    {
        public bool UpdateAfter { get; }
        private readonly IModelRepository _repository;
        private readonly IViewModelService _viewModelService;
        private bool _isLoading = true;
        private bool _hasNonSteamDownloads;
        private bool _showSteamOption;
        private bool _useSteam;
        private bool _downloadEnabled;
        private List<ModStorageSelectionInfo> _mods = new();
        private StorageSelection? _storage;

        public StorageSelection? Storage
        {
            get => _storage;
            set
            {
                if (_storage == value) return;
                _storage = value;
                OnPropertyChanged();
                AdjustSelection();
            }
        }

        public ObservableCollection<StorageSelection> Storages { get; } = new();

        public DelegateCommand Ok { get; }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading == value) return;
                _isLoading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NotIsLoading));
            }
        }

        public bool NotIsLoading => !IsLoading;

        public bool ShowSteamOption
        {
            get => _showSteamOption;
            set
            {
                if (_showSteamOption == value) return;
                _showSteamOption = value;
                OnPropertyChanged();
            }
        }

        public DelegateCommand AddStorage { get; }

        public bool UseSteam
        {
            get => _useSteam;
            set
            {
                if (_useSteam == value) return;
                _useSteam = value;
                OnPropertyChanged();
                DownloadEnabled = !value || _hasNonSteamDownloads;
                AdjustSelection();
            }
        }

        public bool DownloadEnabled
        {
            get => _downloadEnabled;
            set
            {
                if (_downloadEnabled == value) return;
                _downloadEnabled = value;
                OnPropertyChanged();
                AddStorage.SetCanExecute(value);
            }
        }

        public List<ModStorageSelectionInfo> Mods
        {
            get => _mods;
            private set
            {
                if (_mods == value) return;
                _mods = value;
                OnPropertyChanged();
            }
        }


        internal SelectRepositoryStorage(IModelRepository repository, IServiceProvider serviceProvider, bool updateAfter)
        {
            UpdateAfter = updateAfter;
            Ok = new DelegateCommand(HandleOk, !updateAfter);
            AddStorage = new DelegateCommand(HandleAdd, false);
            _repository = repository;
            var model = serviceProvider.Get<IModel>();
            _viewModelService = serviceProvider.Get<IViewModelService>();
            _model = serviceProvider.Get<IModel>();

            _repository.StateChanged += _ => TryLoad(); // TODO: memory leak!
            _eventManager = serviceProvider.Get<IEventManager>();

            _eventManager.Subscribe<AnythingChangedEvent>(HandleAnythingChanged);

            foreach (var storage in model.GetStorages())
            {
                AddWhenLoaded(storage);
            }

            TryLoad();
        }


        public void Dispose()
        {
            _eventManager.Unsubscribe<AnythingChangedEvent>(HandleAnythingChanged);
        }

        private void HandleAnythingChanged(AnythingChangedEvent evt)
        {
            TryLoad();
            Update();
        }

        private void AddStorageToList(IModelStorage storage)
        {
            if (storage.State != LoadingState.Loaded) return;
            storage.StateChanged -= AddWhenLoaded;
            if (!storage.CanWrite || !storage.IsAvailable()) return;
            var storageSelection = new StorageSelection(storage);
            Storages.Add(storageSelection);
            Storage ??= storageSelection;
        }

        private void HandleAdd()
        {
            var storage = _viewModelService.AddStorage();
            if (storage != null)
                AddWhenLoaded(storage);
        }

        private void AddWhenLoaded(IModelStorage storage)
        {
            if (storage.State == LoadingState.Loaded)
                AddStorageToList(storage);
            else
                storage.StateChanged += AddStorageToList;
        }

        private void AdjustSelection()
        {
            if (_repository.State != LoadingState.Loaded) return;

            foreach (var mod in _repository.GetMods())
            {
                var steamUsage = UseSteam
                    ? AutoSelectorCalculation.SteamUsage.UseSteamAndPreferIt
                    : AutoSelectorCalculation.SteamUsage.DontUseSteam;
                var updateSelection = AutoSelectorCalculation.GetAutoSelection(_model, mod, steamUsage, true);
                if (updateSelection != null)
                    mod.SetSelection(updateSelection);
            }
        }

        private void TryLoad()
        {
            if (!IsLoading) return; // only try until we succeed

            if (_repository.State == LoadingState.Error) throw new InvalidOperationException(); // TODO: handle properly

            if (_repository.State == LoadingState.Loading) return;

            var mods = _repository.GetMods();

            if (mods.Any(m => m.GetCurrentSelection() is ModSelectionLoading)) return;

            var selections = mods.Select(m =>
            {
                var selection = m.GetCurrentSelection();
                var action = ModAction.Create(selection, m);
                return (m, action);
            }).ToList();

            UseSteam = ShowSteamOption = selections.Any(s =>
                s.action is SelectMod storageMod &&
                !storageMod.StorageMod.ParentStorage.CanWrite);

            _hasNonSteamDownloads = DownloadEnabled = selections.Any(s => s.action is SelectStorage or SelectNone);
            ShowDownload = DownloadEnabled || ShowSteamOption;
            AddStorage.SetCanExecute(DownloadEnabled);

            IsLoading = false;

            AdjustSelection();

            if (Storage != null)
                Ok.SetCanExecute(true);

            Update();
        }

        private void Update()
        {
            Mods = _repository.GetMods().OrderBy(m => m.Identifier).Select((mod, index) =>
            {
                var selection = mod.GetCurrentSelection();
                var action = ModAction.Create(selection, mod);
                var entry = new ModStorageSelectionInfo(mod.Identifier, action, index % 2);
                return entry;
            }).ToList();

            var isValidSelection = _repository.GetMods().All(mod =>
            {
                var selection = mod.GetCurrentSelection();
                return selection is not ModSelectionNone and not ModSelectionLoading; // TODO: whitelist rather than blacklist? or even some attribute on the selection
            });
            Ok.SetCanExecute(isValidSelection);
        }

        private bool _showDownload;
        private readonly IEventManager _eventManager;
        private readonly IModel _model;

        public bool ShowDownload
        {
            get => _showDownload;
            private set
            {
                if (_showDownload == value) return;
                _showDownload = value;
                OnPropertyChanged();
            }
        }

        private void HandleOk(object? objWindow)
        {
            ((ICloseable)objWindow!).Close(true);
        }

        public record ModStorageSelectionInfo(string ModName, ModAction? Action, int StripeIndex);

        public class StorageSelection
        {
            internal readonly IModelStorage Storage;
            public string Name => Storage.Name;
            public string Location => Storage.GetLocation();

            internal StorageSelection(IModelStorage storage)
            {
                Storage = storage;
            }
        }
    }
}
