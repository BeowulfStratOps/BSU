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

    [Fact]
    private void SlowLoading()
    {
        // repo state should be loading until all mods have a stable result
        // repo details shouldn't ever be sitting on unusable or null while loading.
        // will probably need a loading state to display on repo details per mod

        MainThreadRunner.Run(async () =>
        {
            var model = new ModelBuilder(1000)
            {
                new RepoInfo("repo", true)
                {
                    { "mod1", 1, 1 }
                },
                new StorageInfo("storage", true)
                {
                    { "mod1", 1, 1 }
                }
            }.Build();
            var vm = new ViewModel.ViewModel(model, null!);
            var repoPage = (RepositoriesPage)vm.Content;

            await WaitFor(5000, 100, () =>
            {
                var state = repoPage.Repositories.Single().CalculatedState;

                if (state == CalculatedRepositoryStateEnum.Ready) return true;

                Assert.Equal(CalculatedRepositoryStateEnum.Loading, state);
                return false;
            });
        });
    }
}
