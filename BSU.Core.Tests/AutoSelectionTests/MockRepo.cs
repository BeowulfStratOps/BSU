using System;
using System.Collections.Generic;
using BSU.Core.Launch;
using BSU.Core.Model;
using BSU.Core.Persistence;
using BSU.CoreCommon;
using Moq;

namespace BSU.Core.Tests.AutoSelectionTests;

internal class MockRepo : IModelRepository
{
    public MockRepo(LoadingState state)
    {
        State = state;
    }

    private readonly List<IModelRepositoryMod> _mods = new();

    public IModelRepositoryMod AddMod(int match = 1, int version = 1, LoadingState state = LoadingState.Loaded, PersistedSelection? previousSelection = null)
    {
        var mod = new MockRepoMod(match, version, state, previousSelection);
        _mods.Add(mod);
        return mod;
    }

    public List<IModelRepositoryMod> GetMods() => _mods;

    public Guid Identifier { get; }
    public string Name { get; } = "";
    public LoadingState State { get; }
    public ServerInfo GetServerInfo()
    {
        throw new NotImplementedException();
    }

    public event Action<IModelRepository>? StateChanged;
    public GameLaunchResult? Launch(GlobalSettings settings)
    {
        throw new NotImplementedException();
    }
}
