using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Model;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class SelectRepositoryStorage : ObservableBase
    {
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
                AsyncVoidExecutor.Execute(() => AdjustSelection(CancellationToken.None));
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
                AsyncVoidExecutor.Execute(() => AdjustSelection(CancellationToken.None));
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


        internal SelectRepositoryStorage(IModelRepository repository, IModel model, IViewModelService viewModelService)
        {
            Ok = new DelegateCommand(HandleOk, false);
            AddStorage = new DelegateCommand(HandleAdd, false);
            _repository = repository;
            _model = model;
            _viewModelService = viewModelService;
            AsyncVoidExecutor.Execute(Load);
        }

        private void HandleAdd()
        {
            var storage = _viewModelService.AddStorage(false);
            var selection = new StorageSelection(storage);
            Storages.Add(selection);
            Storage = selection;
        }

        private async Task AdjustSelection(CancellationToken cancellationToken)
        {
            var mods = await _repository.GetMods();
            if (UseSteam)
            {
                foreach (var mod in mods)
                {
                    if (await mod.GetSelection(cancellationToken: cancellationToken) is RepositoryModActionDownload)
                    {
                        await mod.GetSelection(true, cancellationToken);
                    }
                }
            }

            if (Storage != null)
            {
                foreach (var mod in mods)
                {
                    var selection = await mod.GetSelection(cancellationToken: cancellationToken);
                    if (selection is RepositoryModActionDownload ||
                        (selection is RepositoryModActionStorageMod storageMod && !storageMod.StorageMod.CanWrite && !UseSteam))
                        mod.SetSelection(new RepositoryModActionDownload(Storage.Storage));
                }

                Ok.SetCanExecute(true);
            }

            var modInfos = new List<ModStorageSelectionInfo>();
            foreach (var mod in mods)
            {
                var selection = await mod.GetSelection(cancellationToken: CancellationToken.None);
                var action = await ModAction.Create(selection, mod, CancellationToken.None);
                modInfos.Add(new ModStorageSelectionInfo(mod.Identifier, action));
            }

            Mods = modInfos;
        }

        private async Task Load()
        {
            var mods = await _repository.GetMods();
            var selections = (await mods.SelectAsync(async m =>
            {
                var selection = await m.GetSelection(cancellationToken: CancellationToken.None);
                var action = await ModAction.Create(selection, m, CancellationToken.None);
                return (m, action);
            })).ToList();

            UseSteam = ShowSteamOption = selections.Any(s =>
                s.action is SelectMod storageMod &&
                !storageMod.StorageMod.ParentStorage.CanWrite);

            _hasNonSteamDownloads = DownloadEnabled = selections.Any(s => s.action is SelectStorage or null);
            AddStorage.SetCanExecute(DownloadEnabled);

            Mods = selections
                .Select(s =>
                {
                    var (mod, action) = s;
                    return new ModStorageSelectionInfo(mod.Identifier, action);
                }).ToList();

            IsLoading = false;

            if (Storage != null)
                Ok.SetCanExecute(true);

            foreach (var storage in _model.GetStorages())
            {
                if (!storage.CanWrite || !await storage.IsAvailable()) continue;
                var selection = new StorageSelection(storage);
                Storages.Add(selection);
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
            public string Name => Storage.Name;
            public string Location => Storage.GetLocation();

            internal StorageSelection(IModelStorage storage)
            {
                Storage = storage;
            }
        }
    }
}
