using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.CoreCommon;

namespace BSU.Core.Tests.Mocks
{
    internal class MockModelRepositoryMod : IModelRepositoryMod
    {
        private IUpdateState _currentUpdate;
        public UpdateTarget AsUpdateTarget { get; }
        public RepositoryModActionSelection Selection { get; set; }
        public Dictionary<IModelStorageMod, ModAction> LocalMods { get; }
        public bool AllModsLoaded { get; set; }
        public IRepositoryMod Implementation { get; }
        public string DownloadIdentifier { get; set; }
        public string Identifier { get; }
        public event Action<IModelStorageMod> LocalModUpdated;

        public void ChangeAction(IModelStorageMod target, ModActionEnum? newAction)
        {
            throw new NotImplementedException();
        }

        public event Action<IModelStorageMod> ActionAdded;
        public event Action SelectionChanged;
        public event Action DownloadIdentifierChanged;

        public IUpdateState DoUpdate()
        {
            throw new NotImplementedException();
        }

        public void ProcessMod(IModelStorageMod storageMod)
        {
            throw new NotImplementedException();
        }

        public event Action OnUpdateChange;

        public Task<string> GetDisplayName()
        {
            throw new NotImplementedException();
        }

        public void SignalAllStorageModsLoaded()
        {
            throw new NotImplementedException();
        }
    }
}
