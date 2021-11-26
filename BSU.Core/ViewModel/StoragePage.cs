using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.Core.Services;
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
            model.AddedStorage += OnAddStorage;
            model.RemovedStorage += storage =>
            {
                var vmStorage = Storages.Single(r => r.ModelStorage == storage);
                Storages.Remove(vmStorage);
            };
            foreach (var storage in model.GetStorages())
            {
                OnAddStorage(storage);
            }
        }

        private void OnAddStorage(IModelStorage modelStorage)
        {
            var storage = new Storage(modelStorage, _model, _viewModelService);
            Storages.Add(storage);
        }

        internal IModelStorage DoAddStorage(bool allowSteam = true)
        {
            // TODO: could be in a separate class
            var vm = new AddStorage(_model, allowSteam);
            if (!_viewModelService.InteractionService.AddStorage(vm)) return null;
            var type = vm.GetStorageType();
            var name = vm.GetName();
            var path = vm.GetPath();
            var storage = _model.AddStorage(type, path, name);
            return storage;
        }
    }
}
