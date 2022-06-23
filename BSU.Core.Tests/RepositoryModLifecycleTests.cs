using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Events;
using BSU.Core.Ioc;
using BSU.Core.Model;
using BSU.Core.Persistence;
using BSU.Core.Services;
using BSU.Core.Tests.Util;
using BSU.CoreCommon;
using BSU.CoreCommon.Hashes;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests;

public class RepositoryLifecycleTests : LoggedTest
{
    public RepositoryLifecycleTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    private RepositoryMod BuildRepositoryMod(Func<PersistedSelection?> getPersistedSelection, Action<PersistedSelection?> setPersistedSelection)
    {
        var implementation = new Mock<IRepositoryMod>(MockBehavior.Strict);
        implementation.Setup(i => i.GetHashes(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new HashCollection()));
        implementation.Setup(i => i.GetDisplayInfo(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(("", "")));
        implementation.Setup(i => i.GetFileList(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new List<string>()));
        IPersistedRepositoryModState state = new PersistedRepositoryModState(getPersistedSelection, setPersistedSelection);
        var services = new ServiceProvider();
        services.Add<IDispatcher>(null!);
        services.Add<IJobManager>(new TestJobManager());
        services.Add<IEventManager>(new EventManager());
        services.Add<IModActionService>(new ModActionService());
        return new RepositoryMod(implementation.Object, "asdf", state, null!, services);
    }

    [Fact]
    private void ExposePreviousSelection()
    {
        var selection = new PersistedSelection(PersistedSelectionType.StorageMod, Guid.NewGuid(), "qwer");

        var repoMod = BuildRepositoryMod(() => selection, _ => { });

        Assert.Equal(selection, repoMod.GetPreviousSelection());
    }

    [Fact]
    private void SetSelectionHidesPreviousSelection()
    {
        var selection = new PersistedSelection(PersistedSelectionType.StorageMod, Guid.NewGuid(), "qwer");

        var repoMod = BuildRepositoryMod(() => selection, _ => { });

        repoMod.SetSelection(new ModSelectionDisabled());

        Assert.Null(repoMod.GetPreviousSelection());
    }

    [Fact]
    private void SetSelectionSetsPreviousSelection()
    {
        var selection = new PersistedSelection(PersistedSelectionType.StorageMod, Guid.NewGuid(), "qwer");

        PersistedSelection? saved = null;

        var repoMod = BuildRepositoryMod(() => selection, v => { saved = v; });

        repoMod.SetSelection(new ModSelectionDisabled());

        Assert.Equal(PersistedSelection.FromSelection(new ModSelectionDisabled()), saved);
    }
    
    [Fact]
    private void ErrorSetsSelectionToDisabled()
    {
        var implementation = new Mock<IRepositoryMod>(MockBehavior.Strict);
        implementation.Setup(i => i.GetHashes(It.IsAny<CancellationToken>()))
            .Returns(Task.FromException<HashCollection>(new TestException()));
       
        IPersistedRepositoryModState state = new PersistedRepositoryModState(() => null, _ => { });
        var services = new ServiceProvider();
        services.Add<IDispatcher>(null!);
        services.Add<IJobManager>(new TestJobManager());
        services.Add<IEventManager>(new EventManager());
        services.Add<IModActionService>(new ModActionService());
        var repoMod = new RepositoryMod(implementation.Object, "asdf", state, null!, services);

        Assert.Equal(LoadingState.Error, repoMod.State);
        Assert.IsType<ModSelectionDisabled>(repoMod.GetCurrentSelection());
    }
}
