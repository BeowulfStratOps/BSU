using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.Ioc;
using BSU.Core.Model;
using BSU.Core.Services;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class StoragePage : ObservableBase
    {
        private readonly IServiceProvider _services;
        private readonly IModel _model;
        private readonly IInteractionService _interactionService;
        public ObservableCollection<Storage> Storages { get; } = new();
        public DelegateCommand AddStorage { get; }

        public DelegateCommand Back { get; }

        internal StoragePage(IServiceProvider services)
        {
            _services = services;
            Back = new DelegateCommand(_services.Get<IViewModelService>().NavigateBack);
            var model = services.Get<IModel>();
            _model = model;
            _interactionService = services.Get<IInteractionService>();
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
            var storage = new Storage(modelStorage, _services);
            Storages.Add(storage);
        }

        internal IModelStorage? DoAddStorage(bool allowSteam = true)
        {
            // TODO: could be in a separate class
            var vm = new AddStorage(_model, allowSteam);
            if (!_interactionService.AddStorage(vm)) return null;
            var type = vm.GetStorageType();
            var name = vm.GetName();
            var path = vm.GetPath();
            var storage = _model.AddStorage(type, path, name);
            return storage;
        }
    }
}
