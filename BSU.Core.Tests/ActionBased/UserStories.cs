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
        var model = new TestModel();

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
            model.LoadRepository("test", new[] { "mod1" });

            //repo.Load(repoMod);
            //repoMod.Load();

            Assert.False(select.IsLoading);
            select.AddStorage.Execute();
        });


        /*storage.Load();
        Assert.Equal(storage, repoMod.Selection);

        model.Execute(repositoryStorage.Ok, model.Closable);

        update.Finish();

        Assert.Equal(repoMod.Data, storage.Mods[0].Data);*/
    }
}
