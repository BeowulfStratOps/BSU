using System;
using System.Collections.ObjectModel;
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

        private string? _error;
        private readonly IInteractionService _interactionService;
        private readonly IServiceProvider _services;

        internal Storage(IModelStorage storage, IServiceProvider serviceProvider)
        {
            Delete = new DelegateCommand(DoDelete);
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

        private void DoDelete()
        {
            if (!_storage.IsAvailable() || !_storage.CanWrite) // Errored loading, probably because the folder doesn't exist anymore. or steam
            {
                _model.DeleteStorage(_storage, false);
                return;
            }

            // TODO: this doesn't look like it belongs here
            var text = $@"Removing storage {Name}. Do you want to delete the files?

Yes - Delete mods in on this storage
No - Keep mods
Cancel - Do not remove this storage";

            var removeMods =  _interactionService.YesNoCancelPopup(text, "Remove Storage");
            if (removeMods == null) return;


            if (removeMods == true)
            {
                _interactionService.MessagePopup("Removing mods is not supported yet.", "Not supported");
                return;
            }

            _model.DeleteStorage(_storage, (bool) removeMods);
        }
    }
}
