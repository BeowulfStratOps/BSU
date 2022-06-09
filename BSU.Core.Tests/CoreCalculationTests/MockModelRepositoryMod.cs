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

namespace BSU.Core.Tests.CoreCalculationTests
{
    internal class MockModelRepositoryMod : IModelRepositoryMod
    {
        private readonly HashCollection _hashes;
        private ModSelection _selection = new ModSelectionLoading();

        public MockModelRepositoryMod(int match, int version)
        {
            var matchHash = TestUtils.GetMatchHash(match).Result;
            var versionHash = TestUtils.GetVersionHash(version).Result;
            _hashes = new HashCollection(matchHash, versionHash);
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

        public ModSelection GetCurrentSelection() => _selection;

        public event Action<IModelRepositoryMod>? StateChanged;
        public PersistedSelection GetPreviousSelection()
        {
            throw new NotImplementedException();
        }

        public event Action<IModelRepositoryMod>? SelectionChanged;
        public Task<IModHash> GetHash(Type type) => _hashes.GetHash(type);
        public List<Type> GetSupportedHashTypes() => _hashes.GetSupportedHashTypes();
    }
}
