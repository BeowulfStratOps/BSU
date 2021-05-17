using System;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.Core.Tests.Util;
using BSU.CoreCommon;

namespace BSU.Core.Tests.CoreCalculationTests
{
    internal class MockModelStorageMod : IModelStorageMod
    {
        private readonly MatchHash _matchHash;
        private readonly VersionHash _versionHash;
        private readonly StorageModStateEnum _state;
        public bool RequiredMatchHash { get; private set; }
        public bool RequiredVersionHash { get; private set; }

        public MockModelStorageMod(int? match, int? version, StorageModStateEnum state)
        {
            _matchHash = TestUtils.GetMatchHash(match);
            _versionHash = TestUtils.GetVersionHash(version);
            _state = state;
        }

        public event Action StateChanged;

        public IUpdateCreate PrepareUpdate(IRepositoryMod repositoryMod, UpdateTarget target, MatchHash targetMatch,
            VersionHash targetVersion)
        {
            throw new NotImplementedException();
        }

        public void Abort()
        {
            throw new NotImplementedException();
        }

        public PersistedSelection GetStorageModIdentifiers()
        {
            throw new NotImplementedException();
        }

        public bool CanWrite { get; }
        public string Identifier { get; }

        public VersionHash GetVersionHash() => _versionHash;

        public MatchHash GetMatchHash() => _matchHash;

        public StorageModStateEnum GetState() => _state;

        public void RequireMatchHash() => RequiredMatchHash = true;

        public void RequireVersionHash() => RequiredVersionHash = true;
        public void Load()
        {
            throw new NotImplementedException();
        }
    }
}
