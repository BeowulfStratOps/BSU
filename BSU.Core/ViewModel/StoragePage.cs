using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class StoragePage : ObservableBase
    {
        private readonly IModel _model;
        private readonly IViewModelService _viewModelService;
        public ObservableCollection<Storage> Storages { get; } = new();
        public DelegateCommand AddStorage { get; }

        public DelegateCommand Back { get; }

        internal StoragePage(IModel model, IViewModelService viewModelService)
        {
            Back = new DelegateCommand(viewModelService.NavigateBack);
            _model = model;
            _viewModelService = viewModelService;
            AddStorage = new DelegateCommand(() => DoAddStorage());
            foreach (var modelStorage in model.GetStorages())
            {
                var storage = new Storage(modelStorage, model, _viewModelService);
                storage.OnDeleted += StorageOnOnDeleted;
                Storages.Add(storage);
            }
        }

        private void StorageOnOnDeleted(Storage storage)
        {
            Storages.Remove(storage);
            storage.OnDeleted -= StorageOnOnDeleted;
        }

        internal IModelStorage DoAddStorage()
        {
            var vm = new AddStorage(_model);
            if (!_viewModelService.InteractionService.AddStorage(vm)) return null;
            var type = vm.GetStorageType();
            var name = vm.GetName();
            var path = vm.GetPath();
            var storage = _model.AddStorage(type, new DirectoryInfo(path), name);
            var vmStorage = new Storage(storage, _model, _viewModelService);
            Storages.Add(vmStorage);
            vmStorage.OnDeleted += StorageOnOnDeleted;
            return storage;
        }

        public async Task Load()
        {
            await Task.WhenAll(Storages.Select(s => s.Load()));
            await Update();
        }

        public async Task Update()
        {
            await Task.WhenAll(Storages.Select(r => r.Update()));
        }
    }
}
