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
using BSU.Core.Model;
using BSU.Core.Persistence;
using BSU.Core.Tests.Mocks;
using BSU.Core.Tests.Util;
using BSU.Core.ViewModel;
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
        var repoPage = (RepositoriesPage)vm.Content;

        await Task.Delay(100);

        Assert.Equal(CalculatedRepositoryStateEnum.Loading, repoPage.Repositories.Single().CalculatedState);

        load.SetResult();
        await Task.Delay(100);

        Assert.Equal(CalculatedRepositoryStateEnum.Ready, repoPage.Repositories.Single().CalculatedState);

    }
}
