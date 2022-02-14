﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Ioc;
using BSU.Core.Launch;
using BSU.Core.Model;
using BSU.Core.Services;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class RepositoriesPage : ObservableBase
    {
        private readonly IServiceProvider _services;
        private readonly IModel _model;
        private readonly IViewModelService _viewModelService;
        private readonly IInteractionService _interactionService;
        public ObservableCollection<Repository> Repositories { get; } = new();
        public DelegateCommand AddRepository { get; }

        public DelegateCommand ShowStorage { get; }

        internal RepositoriesPage(IServiceProvider services)
        {
            _services = services;
            _viewModelService = services.Get<IViewModelService>();
            _model = services.Get<IModel>();
            _interactionService = services.Get<IInteractionService>();

            ShowStorage = new DelegateCommand(_viewModelService.NavigateToStorages);
            AddRepository = new DelegateCommand(DoAddRepository);
            _model.AddedRepository += repository => AddedRepository(repository);
            _model.RemovedRepository += repository =>
            {
                var vmRepo = Repositories.Single(r => r.ModelRepository == repository);
                Repositories.Remove(vmRepo);
            };
        }

        private void AddedRepository(IModelRepository modelRepository)
        {
            var repository = new Repository(modelRepository, _services);
            Repositories.Add(repository);
        }

        private void DoAddRepository()
        {
            // TODO: use some ioc stuff instead of creating the viewModel explicitly
            var vm = new AddRepository(_model, _services);
            if (!_interactionService.AddRepository(vm)) return;

            var repo = _model.AddRepository("BSO", vm.Url.Trim(), vm.Name.Trim(), Launch.PresetSettings.BuildDefault());

            var vmRepo = _viewModelService.FindVmRepo(repo);

            var settingsVm = new PresetSettings(repo.Settings, false);
            var settingsOk = _interactionService.PresetSettings(settingsVm);

            if (!settingsOk) return;

            repo.Settings = settingsVm.ToLaunchSettings();

            var selectStorageVm = new SelectRepositoryStorage(repo, _services, true);
            if (!_interactionService.SelectRepositoryStorage(selectStorageVm)) return;

            _services.Get<IAsyncVoidExecutor>().Execute(async () =>
            {
                var state = CoreCalculation.GetRepositoryState(repo, _model.GetRepositoryMods());
                if (state == CalculatedRepositoryStateEnum.NeedsSync)
                    await vmRepo.DoUpdate();
            });
        }
    }
}
