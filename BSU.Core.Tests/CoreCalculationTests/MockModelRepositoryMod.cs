using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.Core.Services;
using BSU.Core.Sync;
using BSU.Core.Tests.Util;

namespace BSU.Core.Tests.CoreCalculationTests
{
    internal class MockModelRepositoryMod : IModelRepositoryMod
    {
        private readonly MatchHash _matchHash;
        private readonly VersionHash _versionHash;

        public MockModelRepositoryMod(int? match, int? version)
        {
            _matchHash = TestUtils.GetMatchHash(match).Result;
            _versionHash = TestUtils.GetVersionHash(version).Result;
        }

        public void SetSelection(RepositoryModActionSelection selection)
        {
            throw new NotImplementedException();
        }

        public Task<RepositoryModActionSelection> GetSelection(bool reset = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public string DownloadIdentifier { get; set; }
        public string Identifier { get; }
        public IModelRepository ParentRepository { get; }
        public LoadingState State { get; } = LoadingState.Loaded;
        public Dictionary<IModelStorageMod, List<IModelRepositoryMod>> Conflicts { get; set; } = new();

        public Task<IModUpdate> StartUpdate(IProgress<FileSyncStats> progress, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ModInfo GetModInfo()
        {
            throw new NotImplementedException();
        }

        public MatchHash GetMatchHash() => _matchHash;

        public VersionHash GetVersionHash() => _versionHash;

        public RepositoryModActionSelection GetCurrentSelection()
        {
            throw new NotImplementedException();
        }

        public event Action<IModelRepositoryMod> StateChanged;
        public PersistedSelection GetPreviousSelection()
        {
            throw new NotImplementedException();
        }

        public event Action<IModelRepositoryMod> SelectionChanged;
    }
}
