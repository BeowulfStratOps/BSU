using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.Core.Sync;
using BSU.Core.Tests.Util;

namespace BSU.Core.Tests.AutoSelectionTests;

internal class MockRepoMod : IModelRepositoryMod
{
    private readonly PersistedSelection? _previousSelection;
    private ModSelection _selection = new ModSelectionNone();
    private readonly MatchHash _matchHash;
    private readonly VersionHash _versionHash;

    public MockRepoMod(int match, int version, LoadingState state, PersistedSelection? previousSelection = null)
    {
        _matchHash = TestUtils.GetMatchHash(match).Result;
        _versionHash = TestUtils.GetVersionHash(version).Result;
        State = state;
        _previousSelection = previousSelection;
    }

    public void SetSelection(ModSelection selection)
    {
        _selection = selection;
    }

    public string DownloadIdentifier { get; set; } = null!;
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

    public MatchHash GetMatchHash() => _matchHash;

    public VersionHash GetVersionHash() => _versionHash;

    public ModSelection GetCurrentSelection() => _selection;

    public event Action<IModelRepositoryMod>? StateChanged;
    public PersistedSelection? GetPreviousSelection() => _previousSelection;

    public event Action<IModelRepositoryMod>? SelectionChanged;
    public event Action<IModelRepositoryMod>? DownloadIdentifierChanged;
}
