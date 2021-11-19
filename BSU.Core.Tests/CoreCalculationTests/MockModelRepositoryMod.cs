using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.Core.Model.Updating;
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
        public Dictionary<IModelStorageMod, List<IModelRepositoryMod>> Conflicts { get; set; } = new();

        public Task<IModUpdate> StartUpdate(IProgress<FileSyncStats> progress, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ModInfo> GetModInfo(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<MatchHash> GetMatchHash(CancellationToken cancellationToken)
        {
            return Task.FromResult(_matchHash);
        }

        public Task<VersionHash> GetVersionHash(CancellationToken cancellationToken)
        {
            return Task.FromResult(_versionHash);
        }

        public Task<List<IModelRepositoryMod>> GetConflictsUsingMod(IModelStorageMod mod, CancellationToken cancellationToken)
        {
            var conflicts = Conflicts.TryGetValue(mod, out var mods) ? mods : new List<IModelRepositoryMod>();
            return Task.FromResult(conflicts);
        }

        public Task<ModActionEnum> GetActionForMod(IModelStorageMod storageMod, CancellationToken cancellationToken)
        {
            return CoreCalculation.GetModAction(this, storageMod, cancellationToken);
        }

        public Task<List<(IModelStorageMod mod, ModActionEnum action)>> GetModActions(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetErrorForSelection(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public RepositoryModActionSelection GetCurrentSelection()
        {
            throw new NotImplementedException();
        }
    }
}
