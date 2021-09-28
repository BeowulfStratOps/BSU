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
            AddStorage = new DelegateCommand(DoAddStorage);
            foreach (var modelStorage in model.GetStorages())
            {
                Storages.Add(new Storage(modelStorage, model, _viewModelService));
            }
        }

        private void DoAddStorage()
        {
            var vm = new AddStorage();
            var doAdd = _viewModelService.InteractionService.AddStorage(vm);
            if (doAdd != true) return;
            var storage = _model.AddStorage("DIRECTORY", new DirectoryInfo(vm.Path), vm.Name);
            Storages.Add(new Storage(storage, _model, _viewModelService));
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
