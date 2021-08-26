using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class ViewModel : ObservableBase
    {
        public DelegateCommand AddRepository { get; }
        public DelegateCommand AddStorage { get; }

        private IModel Model { get; }
        public ObservableCollection<Repository> Repositories { get; } = new();
        public ObservableCollection<Storage> Storages { get; } = new();

        public InteractionRequest<AddRepository, bool?> AddRepositoryInteraction { get; } = new();
        public InteractionRequest<AddStorage, bool?> AddStorageInteraction { get; } = new();

        internal ViewModel(IModel model)
        {
            AddRepository = new DelegateCommand(DoAddRepository);
            AddStorage = new DelegateCommand(DoAddStorage);
            Model = model;
            foreach (var modelRepository in model.GetRepositories())
            {
                Repositories.Add(new Repository(modelRepository, model));
            }
            foreach (var modelStorage in model.GetStorages())
            {
                Storages.Add(new Storage(modelStorage, model));
            }
        }

        private async Task DoAddRepository()
        {
            var vm = new AddRepository();
            var doAdd = await AddRepositoryInteraction.Raise(vm);

            if (doAdd != true) return;
            var repo = Model.AddRepository("BSO", vm.Url, vm.Name);
            Repositories.Add(new Repository(repo, Model));
        }

        private async Task DoAddStorage()
        {
            var vm = new AddStorage();
            var doAdd = await AddStorageInteraction.Raise(vm);
            if (doAdd != true) return;
            var storage = Model.AddStorage("DIRECTORY", new DirectoryInfo(vm.Path), vm.Name);
            Storages.Add(new Storage(storage, Model));
        }

        public async Task Load()
        {
            var tasks = new List<Task>();
            tasks.AddRange(Repositories.Select(r => r.Load()));
            tasks.AddRange(Storages.Select(s => s.Load()));
            await Task.WhenAll(tasks);
        }
    }
}
