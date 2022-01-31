using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Launch;
using BSU.Core.Model;
using BSU.Core.Services;
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
            model.AddedRepository += repository => AddedRepository(repository, model, viewModelService);
            model.RemovedRepository += repository =>
            {
                var vmRepo = Repositories.Single(r => r.ModelRepository == repository);
                Repositories.Remove(vmRepo);
            };
            foreach (var repository in model.GetRepositories())
            {
                AddedRepository(repository, model, viewModelService);
            }
        }

        private void AddedRepository(IModelRepository modelRepository, IModel model, IViewModelService viewModelService)
        {
            var repository = new Repository(modelRepository, model, viewModelService);
            Repositories.Add(repository);
        }

        private void DoAddRepository()
        {
            var vm = new AddRepository(_model);
            if (!_viewModelService.InteractionService.AddRepository(vm)) return;

            var repo = _model.AddRepository("BSO", vm.Url.Trim(), vm.Name.Trim(), Launch.PresetSettings.BuildDefault());

            var vmRepo = _viewModelService.FindVmRepo(repo);

            var settingsVm = new PresetSettings(repo.Settings, false);
            var settingsOk = _viewModelService.InteractionService.PresetSettings(settingsVm);

            if (!settingsOk) return;

            repo.Settings = settingsVm.ToLaunchSettings();

            var selectStorageVm = new SelectRepositoryStorage(repo, _model, _viewModelService, true);
            if (!_viewModelService.InteractionService.SelectRepositoryStorage(selectStorageVm)) return;

            AsyncVoidExecutor.Execute(async () =>
            {
                var state = CoreCalculation.GetRepositoryState(repo, _model.GetRepositoryMods());
                if (state == CalculatedRepositoryStateEnum.NeedsSync)
                    await vmRepo.DoUpdate();
            });
        }
    }
}
