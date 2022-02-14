using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.Annotations;
using BSU.Core.Tests.Util;
using BSU.Core.ViewModel;
using BSU.Core.ViewModel.Util;
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
        using var model = new TestModel();

        model.Do<ViewModel.ViewModel>(vm => vm.RepoPage.AddRepository.Execute());

        model.Do<AddRepository>((ar, closeable) =>
        {
            ar.Name = "Test";
            ar.Url = "test";
            ar.Ok.Execute(closeable);
        });

        model.Do<PresetSettings>(ps => ps.UseArmaLauncher = true);
        model.Close(true);

        model.Do<SelectRepositoryStorage>(select =>
        {
            model.LoadRepository("test", new[] { "@mod1" });
            model.LoadRepositoryMod("test", "@mod1", Helper.CreateFiles(1, 1));

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

        model.Do<SelectRepositoryStorage>((select, closeable) =>
        {
            model.LoadStorage("test", Array.Empty<string>());

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

        model.Do<ViewModel.ViewModel>(vm =>
        {
            model.LoadStorageMod("test", "@mod1", new Dictionary<string, byte[]>(), false);
            var repo = vm.RepoPage.Repositories[0];
            model.WaitFor(500, () => repo.UpdateProgress.Active && repo.UpdateProgress.Stage == null);
        });

        model.Do<ViewModel.ViewModel>(_ => model.FinishUpdate("test", "@mod1"));

        model.Do<ViewModel.ViewModel>(vm =>
        {
            var repo = vm.RepoPage.Repositories[0];
            model.WaitFor(50, () => !repo.UpdateProgress.Active);
        });

        model.Do<TestInteractionService.MessagePopupDto>((msg, closeable) => closeable.SetResult(null));

        CheckFileEquality(model.GetRepoFiles("test", "@mod1"), model.GetStorageFiles("test", "@mod1"));

        Assert.Empty(model.GetErrorEvents());
    }

    private static void CheckFileEquality(Dictionary<string,byte[]> files1, Dictionary<string,byte[]> files2)
    {
        Assert.Equal(files1.Keys.ToHashSet(), files2.Keys.ToHashSet());
        foreach (var path in files1.Keys)
        {
            Assert.Equal(files1[path], files2[path]);
        }
    }
}
