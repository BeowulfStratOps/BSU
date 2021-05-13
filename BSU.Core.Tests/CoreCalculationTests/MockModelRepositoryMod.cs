using System;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.Core.Tests.Util;

namespace BSU.Core.Tests.CoreCalculationTests
{
    internal class MockModelRepositoryMod : IModelRepositoryMod
    {
        private readonly MatchHash _matchHash;
        private readonly VersionHash _versionHash;

        public MockModelRepositoryMod(int? match, int? version)
        {
            _matchHash = TestUtils.GetMatchHash(match);
            _versionHash = TestUtils.GetVersionHash(version);
        }

        public UpdateTarget AsUpdateTarget { get; }
        public RepositoryModActionSelection Selection { get; set; }
        public string DownloadIdentifier { get; set; }
        public string Identifier { get; }
        public bool IsLoaded { get; }
        public event Action OnLoaded;
        public event Action<IModelStorageMod> LocalModUpdated;
        public event Action SelectionChanged;
        public IUpdateState DoUpdate()
        {
            throw new NotImplementedException();
        }

        public void ProcessMod(IModelStorageMod storageMod)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetDisplayName()
        {
            throw new NotImplementedException();
        }

        public void SignalAllStorageModsLoaded()
        {
            throw new NotImplementedException();
        }

        public MatchHash GetMatchHash() => _matchHash;

        public VersionHash GetVersionHash() => _versionHash;
    }
}
