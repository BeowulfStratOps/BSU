using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.Core.Sync;
using BSU.Core.Tests.Util;
using BSU.CoreCommon.Hashes;

namespace BSU.Core.Tests.AutoSelectionTests;

internal class MockRepoMod : IModelRepositoryMod
{
    private readonly PersistedSelection? _previousSelection;
    private ModSelection _selection = new ModSelectionNone();
    private readonly HashCollection _hashes;

    public MockRepoMod(int match, int version, LoadingState state, PersistedSelection? previousSelection = null)
    {
        var matchHash = new TestMatchHash(match);
        var versionHash = new TestVersionHash(version);
        _hashes = new HashCollection(matchHash, versionHash);
        State = state;
        _previousSelection = previousSelection;
    }

    public void SetSelection(ModSelection selection)
    {
        _selection = selection;
    }

    public string Identifier { get; } = null!;
    public IModelRepository ParentRepository { get; } = null!;
    public LoadingState State { get; }
    public Task<ModUpdateInfo?> StartUpdate(IProgress<FileSyncStats>? progress, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ModInfo GetModInfo()
    {
        throw new NotImplementedException();
    }


    public ModSelection GetCurrentSelection() => _selection;

    public event Action<IModelRepositoryMod>? StateChanged;
    public PersistedSelection? GetPreviousSelection() => _previousSelection;

    public event Action<IModelRepositoryMod>? SelectionChanged;
    public Task<IModHash> GetHash(Type type) => _hashes.GetHash(type);

    public List<Type> GetSupportedHashTypes() => _hashes.GetSupportedHashTypes();
}
