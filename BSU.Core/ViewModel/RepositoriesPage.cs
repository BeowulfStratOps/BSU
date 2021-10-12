using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
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



        public DelegateCommand ShowStorage { get; }

        internal RepositoriesPage(IModel model, IViewModelService viewModelService)
        {
            ShowStorage = new DelegateCommand(viewModelService.NavigateToStorages);
            _model = model;
            _viewModelService = viewModelService;
            AddRepository = new DelegateCommand(DoAddRepository);
            foreach (var modelRepository in model.GetRepositories())
            {
                var repository = new Repository(modelRepository, model, viewModelService);
                repository.OnDelete += OnDelete;
                Repositories.Add(repository);
            }
        }

        private void OnDelete(Repository repository)
        {
            Repositories.Remove(repository);
            repository.OnDelete -= OnDelete;
        }

        private void DoAddRepository()
        {
            var vm = new AddRepository(_model);
            if (!_viewModelService.InteractionService.AddRepository(vm)) return;

            var repo = _model.AddRepository("BSO", vm.Url.Trim(), vm.Name.Trim());
            var vmRepo = new Repository(repo, _model, _viewModelService);
            Repositories.Add(vmRepo);
            vmRepo.OnDelete += OnDelete;
            AsyncVoidExecutor.Execute(_viewModelService.Update);
            AsyncVoidExecutor.Execute(vmRepo.Load);

            var selectStorageVm = new SelectRepositoryStorage(repo, _model, _viewModelService, true);
            if (!_viewModelService.InteractionService.SelectRepositoryStorage(selectStorageVm)) return;

            AsyncVoidExecutor.Execute(async () =>
            {
                var state = await repo.GetState(CancellationToken.None);
                if (state.State == CalculatedRepositoryStateEnum.NeedsSync)
                    await vmRepo.DoUpdate();
            });
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
