using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.Core.Sync;
using BSU.Core.Tests.Util;

namespace BSU.Core.Tests.CoreCalculationTests
{
    internal class MockModelRepositoryMod : IModelRepositoryMod
    {
        private readonly MatchHash? _matchHash;
        private readonly VersionHash? _versionHash;
        private ModSelection _selection = new ModSelectionLoading();

        public MockModelRepositoryMod(int match, int version)
        {
            _matchHash = TestUtils.GetMatchHash(match).Result;
            _versionHash = TestUtils.GetVersionHash(version).Result;
        }

        public void SetSelection(ModSelection selection)
        {
            _selection = selection;
        }

        public string DownloadIdentifier { get; set; } = null!;
        public string Identifier { get; } = null!;
        public IModelRepository ParentRepository { get; } = null!;
        public LoadingState State { get; } = LoadingState.Loaded;

        public Task<ModUpdateInfo?> StartUpdate(IProgress<FileSyncStats>? progress, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ModInfo GetModInfo()
        {
            throw new NotImplementedException();
        }

        public MatchHash GetMatchHash() => _matchHash ?? throw new InvalidOperationException();

        public VersionHash GetVersionHash() => _versionHash ?? throw new InvalidOperationException();

        public ModSelection GetCurrentSelection() => _selection;

        public event Action<IModelRepositoryMod>? StateChanged;
        public PersistedSelection GetPreviousSelection()
        {
            throw new NotImplementedException();
        }

        public event Action<IModelRepositoryMod>? SelectionChanged;
        public event Action<IModelRepositoryMod>? DownloadIdentifierChanged;
    }
}
