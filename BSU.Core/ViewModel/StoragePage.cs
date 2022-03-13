using System;
using System.Collections.ObjectModel;
using System.Linq;
using BSU.Core.Ioc;
using BSU.Core.Model;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class StoragePage : ObservableBase
    {
        private readonly IServiceProvider _services;
        private readonly IModel _model;
        public ObservableCollection<Storage> Storages { get; } = new();
        public DelegateCommand AddStorage { get; }

        public INavigator Navigator { get; init; }

        internal StoragePage(IServiceProvider services)
        {
            _services = services;
            Navigator = services.Get<INavigator>();
            var model = services.Get<IModel>();
            _model = model;
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

        internal IModelStorage? DoAddStorage()
        {
            // TODO: could be in a separate class
            var dialogResult = _services.Get<IDialogService>().AddStorage();
            if (dialogResult == null) return null;
            return _model.AddStorage(dialogResult.Type, dialogResult.Path, dialogResult.Name);
        }
    }
}
