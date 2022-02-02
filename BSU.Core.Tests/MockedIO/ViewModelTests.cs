using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Ioc;
using BSU.Core.Model;
using BSU.Core.Persistence;
using BSU.Core.Tests.Mocks;
using BSU.Core.Tests.Util;
using BSU.Core.ViewModel;
using Moq;
using NLog;
using NLog.Targets;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests.MockedIO;

public class ViewModelTests : MockedIoTest
{
    public ViewModelTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    [UIFact]
    private async Task SlowLoading()
    {
        // repo state should be loading until all mods have a stable result
        // repo details shouldn't ever be sitting on unusable or null while loading.
        // will probably need a loading state to display on repo details per mod

        var load = new TaskCompletionSource();
        var vm = new ModelBuilder
        {
            new RepoInfo("repo", true, load.Task)
            {
                { "mod1", 1, 1, load.Task }
            },
            new StorageInfo("storage", true, load.Task)
            {
                { "mod1", 1, 1, load.Task }
            }
        }.BuildVm();

        var repoPage = (RepositoriesPage)vm.Navigator.Content;
        var repo = repoPage.Repositories.Single();

        Assert.Equal(CalculatedRepositoryStateEnum.Loading, repo.CalculatedState);

        load.SetResult();

        await WaitFor(50, () => repo.CalculatedState == CalculatedRepositoryStateEnum.Ready);

        Assert.Equal(CalculatedRepositoryStateEnum.Ready, repo.CalculatedState);
    }

    [UIFact]
    private async Task AllowStorageCreationWhenAddingPreset()
    {
        var services = new ServiceProvider();
        var vm = new ModelBuilder
        {
            new RepoInfo("repo")
            {
                { "mod1", 1, 1 }
            }
        }.BuildVm(serviceProvider: services);

        var repoPage = (RepositoriesPage)vm.Navigator.Content;
        var repo = repoPage.Repositories.Single();

        var selectVm = new SelectRepositoryStorage(repo.ModelRepository, services, false);
        await WaitFor(100, () => !selectVm.IsLoading);

        Assert.True(selectVm.ShowDownload);
    }

    [UIFact]
    private async Task AddStorageInSelectRepositoryStorageDialog()
    {
        // TODO: group tests for that dialog

        var dialogService = new Mock<IDialogService>(MockBehavior.Strict);

        var loadTcs = new TaskCompletionSource();

        dialogService.Setup(ds => ds.AddStorage(It.IsAny<bool>()))
            .Returns(new AddStorageDialogResult("TEST", "storage", "storage"));

        var services = new ServiceProvider();
        services.Add(dialogService.Object);

        var vm = new ModelBuilder
        {
            new RepoInfo("repo")
            {
                { "mod1", 1, 1 }
            },
            new StorageInfo("storage", false, loadTcs.Task)
            {
                { "mod1", 1, 1 }
            }
        }.BuildVm(serviceProvider: services);

        var repoPage = (RepositoriesPage)vm.Navigator.Content;
        var repo = repoPage.Repositories.Single();

        var selectVm = new SelectRepositoryStorage(repo.ModelRepository, services, false);
        await WaitFor(100, () => !selectVm.IsLoading);

        selectVm.AddStorage.Execute(null);

        await Task.Delay(100);

        loadTcs.SetResult();

        await Task.Delay(100);

        Assert.Single(selectVm.Storages);
        var repoMod = selectVm.Mods!.Single();
        Assert.IsType<SelectMod>(repoMod.Action);
    }
}
