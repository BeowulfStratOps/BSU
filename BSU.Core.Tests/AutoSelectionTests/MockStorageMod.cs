using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.Core.Sync;
using BSU.Core.Tests.Util;
using BSU.CoreCommon;
using BSU.CoreCommon.Hashes;

namespace BSU.Core.Tests.AutoSelectionTests;

internal class MockStorageMod : IModelStorageMod
{
    private readonly StorageModStateEnum _state;
    private readonly HashCollection _hashes;

    public MockStorageMod(IModelStorage parent, int match, int version, StorageModStateEnum state, bool canWrite,
        string? identifier)
    {
        var matchHash = TestUtils.GetMatchHash(match).Result;
        var versionHash = TestUtils.GetVersionHash(version).Result;
        _hashes = new HashCollection(matchHash, versionHash);
        _state = state;
        ParentStorage = parent;
        Identifier = identifier ?? Guid.NewGuid().ToString();
        CanWrite = canWrite;
    }

    public event Action? StateChanged;

    public Task<UpdateResult> Update(IRepositoryMod repositoryMod, UpdateTarget target, IProgress<FileSyncStats>? progress,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public void Abort()
    {
        throw new NotImplementedException();
    }

    public PersistedSelection GetStorageModIdentifiers()
    {
        return new PersistedSelection(PersistedSelectionType.StorageMod, ParentStorage.Identifier, Identifier);
    }

    public bool CanWrite { get; }
    public string Identifier { get; }
    public IModelStorage ParentStorage { get; }
    public bool IsDeleted { get; }

    public StorageModStateEnum GetState() => _state;

    public Task<Dictionary<string, byte[]>> GetKeyFiles(CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public string GetTitle()
    {
        throw new NotImplementedException();
    }

    public void Delete(bool removeData)
    {
        throw new NotImplementedException();
    }

    public string GetAbsolutePath()
    {
        throw new NotImplementedException();
    }

    public override string ToString() => Identifier;
    public Task<IModHash> GetHash(Type type) => _hashes.GetHash(type);

    public List<Type> GetSupportedHashTypes() => _hashes.GetSupportedHashTypes();
}
