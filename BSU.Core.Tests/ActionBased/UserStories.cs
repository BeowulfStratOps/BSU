using System;
using System.Collections.Generic;
using BSU.Core.Annotations;
using BSU.Core.Tests.ActionBased.TestModel;
using BSU.Core.Tests.Util;
using BSU.Core.ViewModel;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests.ActionBased;

public class UserStories : LoggedTest
{
    public UserStories([NotNull] ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    [Fact]
    private void TestDownload()
    {
        using var model = new TestModel.Model();

        model.Do<ViewModel.ViewModel>(vm => vm.RepoPage.AddRepository.Execute());

        model.Do<AddRepository>((ar, closeable) =>
        {
            ar.Name = "Test";
            ar.Url = "test";
            ar.Ok.Execute(closeable);
        });

        model.Do<PresetSettings>((ps, closeable) =>
        {
            ps.UseArmaLauncher = true;
            closeable.SetResult(true);
        });

        var repo = model.GetRepository("test");
        repo.Load(new[] { "@mod1" });
        var repositoryMod = repo.GetMod("@mod1");
        repositoryMod.Load(FileHelper.CreateFiles(1, 1));

        model.Do<SelectRepositoryStorage>(select =>
        {
            Assert.False(select.IsLoading);
            select.AddStorage.Execute();

            Assert.False(select.Ok.CanExecute(null));
        });

        model.Do<AddStorage>((addStorage, closeable) =>
        {
            addStorage.Name = "asdf";
            addStorage.Path = "test";
            addStorage.Ok.Execute(closeable);
        });

        var storage = model.GetStorage("test");
        storage.Load(Array.Empty<string>());

        model.Do<SelectRepositoryStorage>((select, closeable) =>
        {
            var (modName, modAction) = select.Mods[0];
            Assert.Equal("@mod1", modName);
            Assert.IsType<SelectStorage>(modAction);

            select.Ok.Execute(closeable);
        });

        model.Do<ViewModel.ViewModel>(vm =>
        {
            var repo = vm.RepoPage.Repositories[0];
            Assert.True(repo.UpdateProgress.Active);
            Assert.Equal("Preparing", repo.UpdateProgress.Stage);
        });

        var storageMod = storage.GetMod("@mod1");
        storageMod.Load(new Dictionary<string, byte[]>(), false);

        model.Do<ViewModel.ViewModel>(vm =>
        {
            var repo = vm.RepoPage.Repositories[0];
            model.WaitFor(500, () => repo.UpdateProgress.Active && repo.UpdateProgress.Stage == null);
        });

        repositoryMod.FinishUpdate();

        model.Do<ViewModel.ViewModel>(vm =>
        {
            var repo = vm.RepoPage.Repositories[0];
            model.WaitFor(50, () => !repo.UpdateProgress.Active);
        });

        model.Do<TestInteractionService.MessagePopupDto>((msg, closeable) => closeable.SetResult(null));

        FileHelper.AssertFileEquality(repositoryMod.Files, storageMod.Files);
    }
}
