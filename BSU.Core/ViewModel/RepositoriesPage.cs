using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.Core.Services;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class RepositoriesPage : ObservableBase
    {
        private readonly IModel _model;
        private readonly IViewModelService _viewModelService;
        private readonly Helper _helper;
        public ObservableCollection<Repository> Repositories { get; } = new();
        public DelegateCommand AddRepository { get; }

        public DelegateCommand ShowStorage { get; }

        internal RepositoriesPage(IModel model, IViewModelService viewModelService, Helper helper)
        {
            ShowStorage = new DelegateCommand(viewModelService.NavigateToStorages);
            _model = model;
            _viewModelService = viewModelService;
            _helper = helper;
            AddRepository = new DelegateCommand(DoAddRepository);
            model.AddedRepository += repository => AddedRepository(repository, model, viewModelService);
            // TODO: handler for deleted
        }

        private void AddedRepository(IModelRepository modelRepository, IModel model, IViewModelService viewModelService)
        {
            var repository = new Repository(modelRepository, model, viewModelService, _helper);
            Repositories.Add(repository);
        }

        private void DoAddRepository()
        {
            var vm = new AddRepository(_model);
            if (!_viewModelService.InteractionService.AddRepository(vm)) return;

            var repo = _model.AddRepository("BSO", vm.Url.Trim(), vm.Name.Trim());

            // TODO: this should be handled by the model

            var vmRepo = new Repository(repo, _model, _viewModelService, _helper);

            var selectStorageVm = new SelectRepositoryStorage(repo, _model, _viewModelService, true);
            if (!_viewModelService.InteractionService.SelectRepositoryStorage(selectStorageVm)) return;

            AsyncVoidExecutor.Execute(async () =>
            {
                var state = _helper.GetRepositoryState(repo);
                if (state == CalculatedRepositoryStateEnum.NeedsSync)
                    await vmRepo.DoUpdate();
            });
        }
    }
}
