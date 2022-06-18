using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.Ioc;
using BSU.Core.Model;
using BSU.Core.Services;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class RepositoriesPage : ObservableBase
    {
        private readonly IServiceProvider _services;
        private readonly IModel _model;
        private readonly IInteractionService _interactionService;
        private readonly IRepositoryStateService _repoStateService;
        private readonly IViewModelService _viewModelService;
        public ObservableCollection<Repository> Repositories { get; } = new();
        public DelegateCommand AddRepository { get; }

        public INavigator Navigator { get; init; }

        internal RepositoriesPage(IServiceProvider services, IViewModelService viewModelService)
        {
            _services = services;
            Navigator = viewModelService;
            _viewModelService = viewModelService;
            _model = services.Get<IModel>();
            _repoStateService = services.Get<IRepositoryStateService>();
            _interactionService = services.Get<IInteractionService>();
            var asyncVoidService = services.Get<IAsyncVoidExecutor>();

            AddRepository = new DelegateCommand(() => asyncVoidService.Execute(DoAddRepository));
            _model.AddedRepository += repository => AddedRepository(repository);
            _model.RemovedRepository += repository =>
            {
                var vmRepo = Repositories.Single(r => r.ModelRepository == repository);
                Repositories.Remove(vmRepo);
            };
        }

        private void AddedRepository(IModelRepository modelRepository)
        {
            var repository = new Repository(modelRepository, _services, _viewModelService);
            Repositories.Add(repository);
        }

        private async Task DoAddRepository()
        {
            // TODO: use some ioc stuff instead of creating the viewModel explicitly
            var vm = new AddRepository(_model, _services);
            if (!await _interactionService.AddRepository(vm)) return;

            var repo = _model.AddRepository(vm.RepoType, vm.Url.Trim(), vm.Name.Trim());

            var vmRepo = Repositories.Single(r => r.ModelRepository == repo);

            var selectStorageVm = new SelectRepositoryStorage(repo, _services, true, _viewModelService);
            if (!await _interactionService.SelectRepositoryStorage(selectStorageVm)) return;

            var state = _repoStateService.GetRepositoryState(repo, _model.GetRepositoryMods());
            if (state == CalculatedRepositoryStateEnum.NeedsSync)
                await vmRepo.DoUpdate();
        }
    }
}
