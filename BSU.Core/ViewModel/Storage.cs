using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BSU.Core.Annotations;
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

        public string Error
        {
            get => _error;
            set
            {
                if (_error == value) return;
                _error = value;
                OnPropertyChanged();
            }
        }

        private readonly IViewModelService _viewModelService;
        private string _error;

        internal Storage(IModelStorage storage, IModel model, IViewModelService viewModelService)
        {
            Delete = new DelegateCommand(DoDelete);
            ModelStorage = storage;
            _model = model;
            _viewModelService = viewModelService;
            Identifier = storage.Identifier;
            _storage = storage;
            Name = storage.Name;
            Path = storage.GetLocation();
        }

        internal async Task Load()
        {
            var mods = await ModelStorage.GetMods();
            foreach (var mod in mods)
            {
                Mods.Add(new StorageMod(mod));
            }

            Error = await _storage.IsAvailable() ? null : "Failed to load";
        }

        private async Task DoDelete()
        {
            // TODO: this doesn't look like it belongs here
            var text = $@"Removing storage {Name}. Do you want to delete the files?

Yes - Delete mods in on this storage
No - Keep mods
Cancel - Do not remove this storage";

            var removeMods =  _viewModelService.InteractionService.YesNoCancelPopup(text, "Remove Storage");
            if (removeMods == null) return;
            _model.DeleteStorage(_storage, (bool) removeMods);
        }

        public async Task Update()
        {
            await Task.WhenAll(Mods.ToList().Select(m => m.Update()));
        }
    }
}
