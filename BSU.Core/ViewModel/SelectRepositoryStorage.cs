﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Model;
using BSU.Core.Services;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class SelectRepositoryStorage : ObservableBase
    {
        public bool UpdateAfter { get; }
        private readonly IModelRepository _repository;
        private readonly IModel _model;
        private readonly IViewModelService _viewModelService;
        private bool _isLoading = true;
        private bool _hasNonSteamDownloads;
        private bool _showSteamOption;
        private bool _useSteam;
        private bool _downloadEnabled;
        private List<ModStorageSelectionInfo> _mods;
        private StorageSelection _storage;

        public StorageSelection Storage
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
            set
            {
                if (_mods == value) return;
                _mods = value;
                OnPropertyChanged();
            }
        }


        internal SelectRepositoryStorage(IModelRepository repository, IModel model, IViewModelService viewModelService,
            bool updateAfter)
        {
            UpdateAfter = updateAfter;
            Ok = new DelegateCommand(HandleOk, !updateAfter);
            AddStorage = new DelegateCommand(HandleAdd, false);
            _repository = repository;
            _model = model;
            _viewModelService = viewModelService;
            Load();
        }

        private void HandleAdd()
        {
            var storage = _viewModelService.AddStorage(false);
            if (storage == null) return;
            var selection = new StorageSelection(storage);
            Storages.Add(selection);
            Storage = selection;
        }

        private void AdjustSelection()
        {
            var mods = _repository.GetMods();
            if (UseSteam)
            {
                foreach (var mod in mods)
                {
                    if (mod.GetCurrentSelection() is RepositoryModActionDownload)
                    {
                        mod.SetSelection(null);
                    }
                }
            }

            if (Storage != null)
            {
                foreach (var mod in mods)
                {
                    var selection = mod.GetCurrentSelection();
                    if (selection is RepositoryModActionDownload ||
                        (selection is RepositoryModActionStorageMod storageMod && !storageMod.StorageMod.CanWrite &&
                         !UseSteam))
                    {
                        mod.SetSelection(new RepositoryModActionDownload(Storage.Storage));
                        mod.DownloadIdentifier = Helper.GetAvailableDownloadIdentifier(Storage.Storage, mod.Identifier);
                    }
                }

                Ok.SetCanExecute(true);
            }

            var modInfos = new List<ModStorageSelectionInfo>();
            foreach (var mod in mods.OrderBy(m => m.Identifier))
            {
                var selection = mod.GetCurrentSelection();
                var action = ModAction.Create(selection, mod);
                modInfos.Add(new ModStorageSelectionInfo(mod.Identifier, action));
            }

            Mods = modInfos;
        }

        private void Load()
        {
            var mods = _repository.GetMods();
            var selections = mods.Select(m =>
            {
                var selection = m.GetCurrentSelection();
                var action = ModAction.Create(selection, m);
                return (m, action);
            }).ToList();

            UseSteam = ShowSteamOption = selections.Any(s =>
                s.action is SelectMod storageMod &&
                !storageMod.StorageMod.ParentStorage.CanWrite);

            _hasNonSteamDownloads = DownloadEnabled = selections.Any(s => s.action is SelectStorage or null);
            ShowDownload = DownloadEnabled || ShowSteamOption;
            AddStorage.SetCanExecute(DownloadEnabled);

            Mods = selections
                .Select(s =>
                {
                    var (mod, action) = s;
                    return new ModStorageSelectionInfo(mod.Identifier, action);
                }).OrderBy(t => t.ModName).ToList();

            IsLoading = false;

            if (Storage != null)
                Ok.SetCanExecute(true);

            foreach (var storage in _model.GetStorages())
            {
                if (!storage.CanWrite || !storage.IsAvailable()) continue;
                var selection = new StorageSelection(storage);
                Storages.Add(selection);
            }
        }

        private bool _showDownload;
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

        private void HandleOk(object objWindow)
        {
            ((ICloseable) objWindow).Close(true);
        }

        public record ModStorageSelectionInfo(string ModName, ModAction Action);

        public class StorageSelection
        {
            internal readonly IModelStorage Storage;
            public string Name => Storage?.Name;
            public string Location => Storage?.GetLocation();

            internal StorageSelection(IModelStorage storage)
            {
                Storage = storage;
            }
        }
    }
}
