using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.Model;
using BSU.Core.Tests.ActionBased.TestModel;
using BSU.Core.Tests.Mocks;
using BSU.Core.Tests.Util;
using BSU.Core.ViewModel;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests.ActionBased;

public class UserStories : LoggedTest
{
    public UserStories(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    [Fact]
    private void TestDownload()
    {
        using var model = new TestModel.Model();

        var vm = model.WaitForDialog<ViewModel.ViewModel>();

        model.Do(() => vm.ViewModel.RepoPage.AddRepository.Execute());

        var addRepoDialog = model.WaitForDialog<AddRepository>();

        model.Do(() =>
        {
            addRepoDialog.ViewModel.Name = "Test";
            addRepoDialog.ViewModel.Url = "test";
            addRepoDialog.ViewModel.Ok.Execute(addRepoDialog.Closable);
        });

        var repo = model.GetRepository("test");
        repo.Load(new[] { "@mod1" });
        var repositoryMod = repo.GetMod("@mod1");
        repositoryMod.Load(FileHelper.CreateFiles(1, 1));

        var selectRepositoryStorage = model.WaitForDialog<SelectRepositoryStorage>();

        model.Do(() =>
        {
            Assert.False(selectRepositoryStorage.ViewModel.IsLoading);
            Assert.False(selectRepositoryStorage.ViewModel.Ok.CanExecute(null));
            selectRepositoryStorage.ViewModel.AddStorage.Execute();
        });

        var addStorage = model.WaitForDialog<AddStorage>();

        model.Do(() =>
        {
            addStorage.ViewModel.Name = "asdf";
            addStorage.ViewModel.Path = "test";
            addStorage.ViewModel.Ok.Execute(addStorage.Closable);
        });

        var storage = model.GetStorage("test");
        storage.Load(Array.Empty<string>());

        model.Do(() =>
        {
            var (modName, modAction) = selectRepositoryStorage.ViewModel.Mods[0];
            Assert.Equal("@mod1", modName);
            Assert.IsType<SelectStorage>(modAction);

            selectRepositoryStorage.ViewModel.Ok.Execute(selectRepositoryStorage.Closable);
        });

        var repoVm = vm.ViewModel.RepoPage.Repositories[0];
        model.WaitFor(100, () => repoVm.UpdateProgress.Active);
        Assert.Equal("Preparing", repoVm.UpdateProgress.Stage);

        var storageMod = storage.GetMod("@mod1");
        storageMod.Load(new Dictionary<string, byte[]>(), true);

        repositoryMod.FinishUpdate();

        var popup = model.WaitForDialog<TestInteractionService.MessagePopupDto>(60000);
        model.Do(() => popup.Closable.SetResult(null));

        model.WaitFor(500, () => !repoVm.UpdateProgress.Active);

        FileHelper.AssertFileEquality(repositoryMod.Files, storageMod.Files);

        model.CheckErrorEvents();
    }

    [Fact]
    private void TestLoad()
    {
        using var model = new TestModel.Model(new[] { "repo" }, new[] { "storage" });

        var vm = model.WaitForDialog<ViewModel.ViewModel>();

        var repo = model.GetRepository("repo");
        var storage = model.GetStorage("storage");

        repo.Load(new[] { "@mod" });
        storage.Load(new[] { "@mod" });

        var repoMod = repo.GetMod("@mod");
        var storageMod = storage.GetMod("@mod");

        repoMod.Load(FileHelper.CreateFiles(1, 1));
        storageMod.Load(FileHelper.CreateFiles(1, 1), false);

        var repoVm = vm.ViewModel.RepoPage.Repositories[0];
        var selection = repoVm.Mods[0].Actions.Selection;
        Assert.IsType<SelectMod>(selection);
        Assert.Equal("storage", ((SelectMod)selection).StorageName);
        Assert.Equal(ModActionEnum.Use, ((SelectMod)selection).ActionType);
    }

    [Fact]
    private void TestSlowLoading()
    {
        using var model = new TestModel.Model(new[] { "repo" }, new[] { "storage" });

        var vm = model.WaitForDialog<ViewModel.ViewModel>();

        var repo = model.GetRepository("repo");
        var storage = model.GetStorage("storage");

        var repoVm = vm.ViewModel.RepoPage.Repositories[0];
        Assert.Equal(CalculatedRepositoryStateEnum.Loading, repoVm.CalculatedState);

        repo.Load(new[] { "@mod" });
        storage.Load(new[] { "@mod" });

        var repoMod = repo.GetMod("@mod");
        var storageMod = storage.GetMod("@mod");

        repoMod.Load(FileHelper.CreateFiles(1, 1));
        storageMod.Load(FileHelper.CreateFiles(1, 1), false);

        Assert.Equal(CalculatedRepositoryStateEnum.Ready, repoVm.CalculatedState);
    }

    [Fact]
    private void TestShowStorageError()
    {
        using var model = new TestModel.Model();

        var vm = model.WaitForDialog<ViewModel.ViewModel>();

        model.Do(() => vm.ViewModel.StoragePage.AddStorage.Execute());

        var addStorage = model.WaitForDialog<AddStorage>();

        model.Do(() =>
        {
            addStorage.ViewModel.Name = "asdf";
            addStorage.ViewModel.Path = "test";
            addStorage.ViewModel.Ok.Execute(addStorage.Closable);
        });

        var storage = model.GetStorage("test");
        storage.Load(new TestException());

        Assert.NotNull(vm.ViewModel.StoragePage.Storages.Single(s => s.Name == "test").Error);

        Assert.Single(model.ErrorEvents);
        Assert.Contains("Failed to load storage", model.ErrorEvents[0].Message);
        model.ErrorEvents.Clear();
    }

    [Fact]
    private void TestDownloadWithSteam()
    {
        using var model = new TestModel.Model(new[] { "test" }, new[] { "test" }, false);

        var vm = model.WaitForDialog<ViewModel.ViewModel>();

        model.Do(() => vm.ViewModel.RepoPage.AddRepository.Execute());

        var repo = model.GetRepository("test");
        repo.Load(new[] { "@mod1", "@mod2" });
        var repositoryMod1 = repo.GetMod("@mod1");
        repositoryMod1.Load(FileHelper.CreateFiles(1, 1));
        var repositoryMod2 = repo.GetMod("@mod2");
        repositoryMod2.Load(FileHelper.CreateFiles(2, 2));

        var storage = model.GetStorage("test");
        storage.Load(Array.Empty<string>());

        var steam = model.GetStorage("steam");
        steam.Load(new[] { "@mod2" });
        var steamMod = steam.GetMod("@mod2");
        steamMod.Load(FileHelper.CreateFiles(2, 2), false);

        model.Do(() =>
        {
            vm.ViewModel.RepoPage.Repositories[0].ChooseDownloadLocation.Execute();
        });

        var selectRepositoryStorage = model.WaitForDialog<SelectRepositoryStorage>();

        var sVm = selectRepositoryStorage.ViewModel;
        Assert.False(sVm.IsLoading);
        Assert.True(sVm.Ok.CanExecute(null));
        Assert.True(sVm.ShowSteamOption);
        Assert.True(sVm.UseSteam);

        model.Do(() =>
        {
            sVm.UseSteam = false;
        });

        Assert.True(sVm.ShowSteamOption);
        Assert.False(sVm.UseSteam);
    }
}
