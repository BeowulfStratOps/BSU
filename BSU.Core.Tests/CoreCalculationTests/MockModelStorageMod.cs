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
using BSU.CoreCommon;

namespace BSU.Core.Tests.CoreCalculationTests
{
    internal class MockModelStorageMod : IModelStorageMod
    {
        private readonly MatchHash? _matchHash;
        private readonly VersionHash? _versionHash;
        private readonly StorageModStateEnum _state;

        public MockModelStorageMod(int? match, int? version, StorageModStateEnum state)
        {
            _matchHash = match != null ? TestUtils.GetMatchHash((int)match).Result : null;
            _versionHash = version != null ? TestUtils.GetVersionHash((int)version).Result : null;
            _state = state;
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
            throw new NotImplementedException();
        }

        public bool CanWrite { get; set; } = true;
        public string Identifier { get; } = null!;
        public IModelStorage ParentStorage { get; } = null!;
        public bool IsDeleted { get; }
        public VersionHash GetVersionHash() => _versionHash ?? throw new InvalidOperationException();

        public MatchHash GetMatchHash() => _matchHash ?? throw new InvalidOperationException();


        public StorageModStateEnum GetState() => _state;
        public string GetTitle()
        {
            throw new NotImplementedException();
        }

        public void Delete(bool removeData)
        {
            throw new NotImplementedException();
        }
    }
}
