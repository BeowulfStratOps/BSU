using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using BSU.Core.JobManager;
using BSU.Core.Model;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class ViewModel : ObservableBase
    {
        public DelegateCommand AddRepository { get; }
        public DelegateCommand AddStorage { get; }
        
        private Model.Model Model { get; }
        public ObservableCollection<Repository> Repositories { get; } = new ObservableCollection<Repository>();
        public ObservableCollection<Storage> Storages { get; } = new ObservableCollection<Storage>();

        public InteractionRequest<AddRepository, bool?> AddRepositoryInteraction { get; } = new InteractionRequest<AddRepository, bool?>();
        public InteractionRequest<AddStorage, bool?> AddStorageInteraction { get; } = new InteractionRequest<AddStorage, bool?>();

        internal ViewModel(Model.Model model)
        {
            AddRepository = new DelegateCommand(DoAddRepository);
            AddStorage = new DelegateCommand(DoAddStorage);
            Model = model;
            model.RepositoryAdded += repository => Repositories.Add(new Repository(repository, model));
            model.RepositoryDeleted += repository =>
            {
                var vmRepository = Repositories.Single(r => r.Identifier == repository.Identifier);
                Repositories.Remove(vmRepository);
            };
            model.StorageAdded += storage =>
            {
                Storages.Add(new Storage(storage, model));
                foreach (var repository in Repositories)
                {
                    foreach (var mod in repository.Mods)
                    {
                        mod.AddStorage(storage);
                    }
                }
            };
            model.StorageDeleted += storage =>
            {
                var vmStorage = Storages.Single(s => s.Identifier == storage.Identifier);
                Storages.Remove(vmStorage);
                foreach (var repository in Repositories)
                {
                    foreach (var mod in repository.Mods)
                    {
                        mod.RemoveStorage(storage);
                    }
                }
            };

        }

        private void DoAddRepository()
        {
            var vm = new AddRepository();
            AddRepositoryInteraction.Raise(vm, b =>
            {
                if (b != true) return;
                Model.AddRepository("BSO", vm.Url, vm.Name);
            });
        }

        private void DoAddStorage()
        {
            var vm = new AddStorage();
            AddStorageInteraction.Raise(vm, b =>
            {
                if (b != true) return;
                Model.AddStorage("DIRECTORY", new DirectoryInfo(vm.Path), vm.Name);
            });
        }
    }
}
