using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.Core.Sync;
using BSU.Core.Tests.Util;
using BSU.CoreCommon;

namespace BSU.Core.Tests.AutoSelectionTests;

internal class MockStorageMod : IModelStorageMod
{
    private readonly StorageModStateEnum _state;
    private readonly MatchHash _matchHash;
    private readonly VersionHash _versionHash;

    public MockStorageMod(IModelStorage parent, int match, int version, StorageModStateEnum state, bool canWrite,
        string? identifier)
    {
        _matchHash = TestUtils.GetMatchHash(match).Result;
        _versionHash = TestUtils.GetVersionHash(version).Result;
        _state = state;
        ParentStorage = parent;
        Identifier = identifier ?? Guid.NewGuid().ToString();
        CanWrite = canWrite;
    }

    public event Action<IModelStorageMod>? StateChanged;

    public Task<UpdateResult> Update(IRepositoryMod repositoryMod, MatchHash targetMatch, VersionHash targetVersion, IProgress<FileSyncStats>? progress,
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
    public VersionHash GetVersionHash() => _versionHash;

    public MatchHash GetMatchHash() => _matchHash;

    public StorageModStateEnum GetState() => _state;

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
}
