using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class RepositoriesPage : ObservableBase
    {
        private readonly IModel _model;
        private readonly IViewModelService _viewModelService;
        public ObservableCollection<Repository> Repositories { get; } = new();
        public DelegateCommand AddRepository { get; }
        public InteractionRequest<AddRepository, bool?> AddRepositoryInteraction { get; } = new();



        public DelegateCommand ShowStorage { get; }

        internal RepositoriesPage(IModel model, IViewModelService viewModelService)
        {
            ShowStorage = new DelegateCommand(viewModelService.NavigateToStorages);
            _model = model;
            _viewModelService = viewModelService;
            AddRepository = new DelegateCommand(DoAddRepository);
            foreach (var modelRepository in model.GetRepositories())
            {
                Repositories.Add(new Repository(modelRepository, model, viewModelService));
            }
        }

        private async Task DoAddRepository()
        {
            var vm = new AddRepository();
            var doAdd = await AddRepositoryInteraction.Raise(vm);

            if (doAdd != true) return;
            var repo = _model.AddRepository("BSO", vm.Url, vm.Name);
            Repositories.Add(new Repository(repo, _model, _viewModelService));
        }

        public async Task Update()
        {
            await Task.WhenAll(Repositories.Select(r => r.UpdateMods()));
        }

        public async Task Load()
        {
            async Task LoadAndUpdate(Repository repository)
            {
                await repository.Load();
                await repository.UpdateMods();
            }

            await Task.WhenAll(Repositories.Select(LoadAndUpdate));
        }
    }
}
