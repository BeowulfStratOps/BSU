using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.Core.Tests.Util;
using BSU.Core.Tests.ViewModelIntegration.TestModel;
using BSU.Core.ViewModel;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests.ViewModelIntegration;

public class UserStories : LoggedTest
{
    public UserStories(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    [StaFact]
    private async Task TestDownload()
    {
        var model = new ViewModelIntegration.TestModel.Model(OutputHelper);

        var vm = await model.WaitForDialog<ViewModel.ViewModel>();
        model.GetStorage("steam").Load();

        vm.ViewModel.RepoPage.AddRepository.Execute();

        var addRepoDialog = await model.WaitForDialog<AddRepository>();

        addRepoDialog.ViewModel.Name = "Test";
        addRepoDialog.ViewModel.Url = "test";
        addRepoDialog.ViewModel.RepoType = "BSO";
        addRepoDialog.ViewModel.Ok.Execute(addRepoDialog.Closable);

        var repo = model.GetRepository("test");
        repo.Load("@mod1");
        var repositoryMod = repo.GetMod("@mod1");
        repositoryMod.Load(FileHelper.CreateFiles(1, 1));

        var selectRepositoryStorage = await model.WaitForDialog<SelectRepositoryStorage>();

        Assert.False(selectRepositoryStorage.ViewModel.IsLoading);
        Assert.False(selectRepositoryStorage.ViewModel.Ok.CanExecute(null));
        selectRepositoryStorage.ViewModel.AddStorage.Execute();

        var addStorage = await model.WaitForDialog<AddStorage>();

        addStorage.ViewModel.Name = "asdf";
        addStorage.ViewModel.Path = "test";
        addStorage.ViewModel.Ok.Execute(addStorage.Closable);

        var storage = model.GetStorage("test");
        storage.Load(Array.Empty<string>());

        var (modName, modAction, _) = selectRepositoryStorage.ViewModel.Mods[0];
        Assert.Equal("@mod1", modName);
        Assert.IsType<SelectStorage>(modAction);

        selectRepositoryStorage.ViewModel.Ok.Execute(selectRepositoryStorage.Closable);

        var repoVm = vm.ViewModel.RepoPage.Repositories[0];
        Assert.True(repoVm.UpdateProgress.Active);
        Assert.Equal("Preparing", repoVm.UpdateProgress.Stage);

        var storageMod = storage.GetMod("@mod1");
        storageMod.Load(new Dictionary<string, byte[]>());

        repositoryMod.FinishUpdate();

        var popup = await model.WaitForDialog<TestInteractionService.MessagePopupDto>();
        popup.Closable.Close(true);

        Assert.False(repoVm.UpdateProgress.Active);

        FileHelper.AssertFileEquality(repositoryMod.Files, storageMod.Files);
    }

    [StaFact]
    private async Task TestLoad()
    {
        await using var model = new ViewModelIntegration.TestModel.Model(OutputHelper, new[] { "repo" }, new[] { "storage" });

        var vm = await model.WaitForDialog<ViewModel.ViewModel>();

        var repo = model.GetRepository("repo");
        
        model.GetStorage("steam").Load();
        var storage = model.GetStorage("storage");

        repo.Load("@mod");
        storage.Load("@mod");

        var repoMod = repo.GetMod("@mod");
        var storageMod = storage.GetMod("@mod");

        repoMod.Load(FileHelper.CreateFiles(1, 1));
        storageMod.Load(FileHelper.CreateFiles(1, 1));

        var repoVm = vm.ViewModel.RepoPage.Repositories[0];
        var selection = repoVm.Mods[0].Actions.Selection;
        Assert.IsType<SelectMod>(selection);
        Assert.Equal("storage", ((SelectMod)selection).StorageName);
        Assert.Equal(ModActionEnum.Use, ((SelectMod)selection).ActionType);
        
        Assert.Empty(model.ErrorEvents);
    }

    [StaFact]
    private async Task TestSlowLoading()
    {
        var model = new ViewModelIntegration.TestModel.Model(OutputHelper, new[] { "repo" }, new[] { "storage" });

        var vm = await model.WaitForDialog<ViewModel.ViewModel>();

        var repo = model.GetRepository("repo");
        var storage = model.GetStorage("storage");

        var repoVm = vm.ViewModel.RepoPage.Repositories[0];
        Assert.Equal(CalculatedRepositoryStateEnum.Loading, repoVm.CalculatedState);

        repo.Load("@mod");
        storage.Load("@mod");
        model.GetStorage("steam").Load();

        var repoMod = repo.GetMod("@mod");
        var storageMod = storage.GetMod("@mod");

        repoMod.Load(FileHelper.CreateFiles(1, 1));
        storageMod.Load(FileHelper.CreateFiles(1, 1));

        Assert.Equal(CalculatedRepositoryStateEnum.Ready, repoVm.CalculatedState);
    }

    [StaFact]
    private async Task TestShowStorageError()
    {
        var model = new ViewModelIntegration.TestModel.Model(OutputHelper);
        model.GetStorage("steam").Load();

        var vm = await model.WaitForDialog<ViewModel.ViewModel>();

       vm.ViewModel.StoragePage.AddStorage.Execute();

        var addStorage = await model.WaitForDialog<AddStorage>();

        addStorage.ViewModel.Name = "asdf";
        addStorage.ViewModel.Path = "test";
        addStorage.ViewModel.Ok.Execute(addStorage.Closable);

        var storage = model.GetStorage("test");
        storage.Load(new TestException());

        Assert.NotNull(vm.ViewModel.StoragePage.Storages.Single(s => s.Name == "test").Error);

        Assert.Single(model.ErrorEvents);
        Assert.Contains("Failed to load storage", model.ErrorEvents[0].Message);
        model.ErrorEvents.Clear();
    }

    [StaFact]
    private async Task TestDownloadWithSteam()
    {
        var model = new ViewModelIntegration.TestModel.Model(OutputHelper, new[] { "test" }, new[] { "test" });

        var vm = await model.WaitForDialog<ViewModel.ViewModel>();

        vm.ViewModel.RepoPage.AddRepository.Execute();

        var repo = model.GetRepository("test");
        repo.Load("@mod1", "@mod2");
        var repositoryMod1 = repo.GetMod("@mod1");
        repositoryMod1.Load(FileHelper.CreateFiles(1, 1));
        var repositoryMod2 = repo.GetMod("@mod2");
        repositoryMod2.Load(FileHelper.CreateFiles(2, 2));

        var storage = model.GetStorage("test");
        storage.Load(Array.Empty<string>());

        var steam = model.GetStorage("steam");
        steam.Load("@mod2");
        var steamMod = steam.GetMod("@mod2");
        steamMod.Load(FileHelper.CreateFiles(2, 2));

        vm.ViewModel.RepoPage.Repositories[0].ChooseDownloadLocation.Execute();

        var selectRepositoryStorage = await model.WaitForDialog<SelectRepositoryStorage>();

        var sVm = selectRepositoryStorage.ViewModel;
        Assert.False(sVm.IsLoading);
        Assert.True(sVm.Ok.CanExecute(null));

        sVm.UseSteam = false;

        Assert.False(sVm.UseSteam);

        // TODO: make sure the download actually works?
    }
}
