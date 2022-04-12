using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Launch;
using BSU.Core.Model;
using BSU.CoreCommon;

namespace BSU.Core.Tests.AutoSelectionTests;

internal class MockModel : IModel
{
    private readonly List<IModelStorage> _storages = new();
    private readonly List<IModelRepository> _repositories = new();

    public void DeleteRepository(IModelRepository repository, bool removeMods)
    {
        throw new NotImplementedException();
    }

    public void DeleteStorage(IModelStorage storage, bool removeMods)
    {
        throw new NotImplementedException();
    }

    public IModelRepository AddRepository(string type, string url, string name)
    {
        throw new NotImplementedException();
    }

    public IModelStorage AddStorage(string type, string path, string name)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<IModelStorage> GetStorages() => _storages;

    public IEnumerable<IModelRepository> GetRepositories() => _repositories;

    public Task<ServerInfo?> CheckRepositoryUrl(string url, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public event Action<IModelRepository>? AddedRepository;
    public event Action<IModelStorage>? AddedStorage;
    public List<IModelStorageMod> GetStorageMods() => _storages.Where(s => s.State == LoadingState.Loaded)
    .SelectMany(s => s.GetMods()).ToList();

    public List<IModelRepositoryMod> GetRepositoryMods() => _repositories.Where(r => r.State == LoadingState.Loaded)
        .SelectMany(r => r.GetMods()).ToList();

    public event Action<IModelRepository>? RemovedRepository;
    public event Action<IModelStorage>? RemovedStorage;
    public GlobalSettings GetSettings()
    {
        throw new NotImplementedException();
    }

    public void SetSettings(GlobalSettings globalSettings)
    {
        throw new NotImplementedException();
    }

    public MockRepo CreateRepo(LoadingState state = LoadingState.Loaded)
    {
        var repo = new MockRepo(state);
        _repositories.Add(repo);
        return repo;
    }

    public MockStorage CreateStorage(LoadingState state = LoadingState.Loaded)
    {
        var storage = new MockStorage(state);
        _storages.Add(storage);
        return storage;
    }
}
