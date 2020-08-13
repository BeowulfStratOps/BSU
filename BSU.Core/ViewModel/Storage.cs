using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BSU.Core.Annotations;

namespace BSU.Core.ViewModel
{
    public class Storage : ViewModelClass
    {
        private readonly ViewModel _viewModel;
        public string Name { get; }
        internal Model.Storage ModelStorage { get; }
        
        public bool IsLoading { get; }

        public ObservableCollection<StorageMod> Mods { get; } = new ObservableCollection<StorageMod>();

        internal Storage(Model.Storage storage, ViewModel viewModel)
        {
            IsLoading = storage.Loading.IsActive();
            ModelStorage = storage;
            _viewModel = viewModel;
            Name = storage.Identifier;
            storage.ModAdded += mod => Mods.Add(new StorageMod(mod, viewModel));
        }
    }
}
