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
        public string UpdateText { get; }
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
            UpdateText = updateAfter ? "Sync" : "OK";
            Ok = new DelegateCommand(HandleOk, !updateAfter);
            AddStorage = new DelegateCommand(HandleAdd, false);
            _repository = repository;
            var model = serviceProvider.Get<IModel>();
            _viewModelService = serviceProvider.Get<IViewModelService>();

            TryLoad();
            _repository.StateChanged += _ => TryLoad();
            _eventManager = serviceProvider.Get<IEventManager>();

            _eventManager.Subscribe<AnythingChangedEvent>(HandleAnythingChanged);

            foreach (var storage in model.GetStorages())
            {
                AddStorageToList(storage);
            }
            model.AddedStorage += AddStorageToList;
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
            if (!storage.CanWrite || !storage.IsAvailable()) return;
            Storages.Add(new StorageSelection(storage));
        }

        private void HandleAdd()
        {
            var storage = _viewModelService.AddStorage(false);
            if (storage == null) return;

            if (storage.State == LoadingState.Loaded)
                Storage = new StorageSelection(storage);
            else
                storage.StateChanged += HandleAddWhenLoaded;
        }

        private void HandleAddWhenLoaded(IModelStorage storage)
        {
            if (storage.State != LoadingState.Loaded) return;
            storage.StateChanged -= HandleAddWhenLoaded;
            Storage = new StorageSelection(storage);
        }

        private void AdjustSelection()
        {
            // TODO: better interface functions
            if (IsLoading) return;

            var mods = _repository.GetMods();
            if (UseSteam)
            {
                foreach (var mod in mods)
                {
                    if (mod.GetCurrentSelection() is ModSelectionDownload)
                    {
                        mod.SetSelection(new ModSelectionNone());
                    }
                }
            }

            if (Storage == null) return;

            foreach (var mod in mods)
            {
                var selection = mod.GetCurrentSelection();
                if (selection is ModSelectionDownload ||
                    (selection is ModSelectionStorageMod storageMod && !storageMod.StorageMod.CanWrite &&
                     !UseSteam))
                {
                    mod.SetSelection(new ModSelectionDownload(Storage.Storage));
                    mod.DownloadIdentifier = CoreCalculation.GetAvailableDownloadIdentifier(Storage.Storage, mod.Identifier);
                }
            }

            Ok.SetCanExecute(true);
        }

        private void TryLoad()
        {
            if (_repository.State == LoadingState.Error) throw new InvalidOperationException(); // TODO: handle properly

            if (_repository.State == LoadingState.Loading) return;

            var mods = _repository.GetMods();

            if (mods.Any(m => m.GetCurrentSelection() is ModSelectionLoading)) return;

            // TODO: Unsubscribe TryLoad from events.

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

            if (Storage != null)
                Ok.SetCanExecute(true);

            Update();
        }

        private void Update()
        {
            Mods = _repository.GetMods().Select(mod =>
            {
                var selection = mod.GetCurrentSelection();
                var action = ModAction.Create(selection, mod);
                var entry = new ModStorageSelectionInfo(mod.Identifier, action);
                return entry;
            }).OrderBy(t => t.ModName).ToList();
        }

        private bool _showDownload;
        private readonly IEventManager _eventManager;

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

        public record ModStorageSelectionInfo(string ModName, ModAction? Action);

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
