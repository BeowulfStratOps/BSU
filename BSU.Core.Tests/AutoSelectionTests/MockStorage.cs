using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.Core.Persistence;
using BSU.CoreCommon.Hashes;
using Moq;

namespace BSU.Core.Tests.AutoSelectionTests;

internal class MockStorage : IModelStorage
{
    public MockStorage(LoadingState state)
    {
        Identifier = Guid.NewGuid();
        State = state;
    }

    private readonly List<IModelStorageMod> _mods = new();

    public IModelStorageMod AddMod(int match = 1, int version = 1, StorageModStateEnum state = StorageModStateEnum.Created, bool canWrite = true, string? identifier = null)
    {
        var mod = new MockStorageMod(this, match, version, state, canWrite, identifier);
        _mods.Add(mod);
        return mod;
    }

    public List<IModelStorageMod> GetMods() => _mods;
    public Task<IModelStorageMod> CreateMod(string identifier, HashCollection hashes)
    {
        throw new NotImplementedException();
    }

    public bool CanWrite { get; }
    public Guid Identifier { get; }
    public string Name { get; } = "";
    public bool IsDeleted { get; }
    public LoadingState State { get; }
    public PersistedSelection AsStorageIdentifier()
    {
        throw new NotImplementedException();
    }

    public bool HasMod(string downloadIdentifier)
    {
        throw new NotImplementedException();
    }

    public string GetLocation()
    {
        throw new NotImplementedException();
    }

    public bool IsAvailable()
    {
        throw new NotImplementedException();
    }

    public void Delete(bool removeMods)
    {
        throw new NotImplementedException();
    }

    public event Action<IModelStorage>? StateChanged;
    public event Action<IModelStorageMod>? AddedMod;
}
