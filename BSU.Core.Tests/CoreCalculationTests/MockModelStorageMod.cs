﻿using System;
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
        private readonly MatchHash _matchHash;
        private readonly VersionHash _versionHash;
        private readonly StorageModStateEnum _state;
        public bool RequiredMatchHash { get; private set; }
        public bool RequiredVersionHash { get; private set; }

        public MockModelStorageMod(int? match, int? version, StorageModStateEnum state)
        {
            _matchHash = TestUtils.GetMatchHash(match).Result;
            _versionHash = TestUtils.GetVersionHash(version).Result;
            _state = state;
        }

        public event Action StateChanged;

        public Task<IModUpdate> PrepareUpdate(IRepositoryMod repositoryMod, MatchHash targetMatch,
            VersionHash targetVersion, IProgress<FileSyncStats> progress)
        {
            throw new NotImplementedException();
        }

        public Task Abort()
        {
            throw new NotImplementedException();
        }

        public PersistedSelection GetStorageModIdentifiers()
        {
            throw new NotImplementedException();
        }

        public bool CanWrite { get; set; } = true;
        public string Identifier { get; }
        public IModelStorage ParentStorage { get; }
        public bool IsDeleted { get; }

        public Task<VersionHash> GetVersionHash(CancellationToken cancellationToken)
        {
            RequiredVersionHash = true;
            return Task.FromResult(_versionHash);
        }

        public Task<MatchHash> GetMatchHash(CancellationToken cancellationToken)
        {
            RequiredMatchHash = true;
            return Task.FromResult(_matchHash);
        }

        public StorageModStateEnum GetState() => _state;
        public Task<IEnumerable<IModelRepositoryMod>> GetUsedBy(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public CancellationToken GetStateToken()
        {
            return new CancellationToken();
        }

        public Task<string> GetTitle(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task Delete(bool removeData)
        {
            throw new NotImplementedException();
        }
    }
}
