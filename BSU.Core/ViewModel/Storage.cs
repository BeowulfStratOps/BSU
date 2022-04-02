using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using BSU.Core.Ioc;
using BSU.Core.Model;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class Storage : ObservableBase
    {
        private readonly IModelStorage _storage;
        private readonly IModel _model;
        public string Name { get; }
        internal IModelStorage ModelStorage { get; }

        public ObservableCollection<StorageMod> Mods { get; } = new();

        public DelegateCommand Delete { get; }
        public DelegateCommand ToggleShowMods { get; }
        public Guid Identifier { get; }

        public string Path { get; }

        public string? Error
        {
            get => _error;
            set
            {
                if (_error == value) return;
                _error = value;
                OnPropertyChanged();
            }
        }

        private bool _isShowingMods;
        public bool IsShowingMods
        {
            get => _isShowingMods;
            set
            {
                if (SetProperty(ref _isShowingMods, value))
                    OnPropertyChanged(nameof(NotIsShowingMods));
            }
        }

        public bool NotIsShowingMods => !IsShowingMods;

        public bool CanWrite { get; }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        private string? _error;
        private readonly IInteractionService _interactionService;
        private readonly IServiceProvider _services;

        internal Storage(IModelStorage storage, IServiceProvider serviceProvider)
        {
            _isLoading = storage.State == LoadingState.Loading;
            CanWrite = storage.CanWrite;
            Delete = new DelegateCommand(DoDelete, storage.State != LoadingState.Loading);
            ToggleShowMods = new DelegateCommand(() => IsShowingMods = !IsShowingMods);
            ModelStorage = storage;
            var model = serviceProvider.Get<IModel>();
            _model = model;
            Identifier = storage.Identifier;
            _storage = storage;
            Name = storage.Name;
            Path = storage.GetLocation();
            storage.StateChanged += _ => OnStateChanged();
            _interactionService = serviceProvider.Get<IInteractionService>();
            _services = serviceProvider;
        }

        private void OnStateChanged()
        {
            Delete.SetCanExecute(_storage.State != LoadingState.Loading);
            IsLoading = _storage.State == LoadingState.Loading;

            if (_storage.State == LoadingState.Error)
            {
                Error = "Failed to load";
                return;
            }

            foreach (var mod in ModelStorage.GetMods())
            {
                Mods.Add(new StorageMod(mod, _services));
            }
        }

        private enum DeletionTypeEnum
        {
            Cancel = 0,
            DeleteMods,
            KeepMods
        }

        private void DoDelete()
        {
            if (!_storage.IsAvailable() || !_storage.CanWrite) // Errored loading, probably because the folder doesn't exist anymore. or steam
            {
                _model.DeleteStorage(_storage, false);
                return;
            }

            // TODO: this doesn't look like it belongs here
            var text = $@"Removing storage {Name}. Do you want to delete the files?";

            var options = new Dictionary<DeletionTypeEnum, string>
            {
                { DeletionTypeEnum.DeleteMods, "Delete mods in this storage" },
                { DeletionTypeEnum.KeepMods, "Keep mods" },
                { DeletionTypeEnum.Cancel, "Cancel" },
            };

            var removeMods =  _interactionService.OptionsPopup(text, "Remove Storage", options, MessageImageEnum.Question);
            if (removeMods == DeletionTypeEnum.Cancel) return;


            if (removeMods == DeletionTypeEnum.DeleteMods)
            {
                _interactionService.MessagePopup("Removing mods is not supported yet.", "Not supported", MessageImageEnum.Error);
                return;
            }

            _model.DeleteStorage(_storage, removeMods == DeletionTypeEnum.DeleteMods);
        }
    }
}
