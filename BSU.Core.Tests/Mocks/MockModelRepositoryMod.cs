using System;
using System.Collections.Generic;
using BSU.Core.Model;
using BSU.CoreCommon;

namespace BSU.Core.Tests.Mocks
{
    internal class MockModelRepositoryMod : IModelRepositoryMod
    {
        private IUpdateState _currentUpdate;
        public UpdateTarget AsUpdateTarget { get; }
        public RepositoryModActionSelection Selection { get; set; }
        public Dictionary<IModelStorageMod, ModAction> Actions { get; }
        public bool AllModsLoaded { get; set; }
        public IRepositoryMod Implementation { get; }
        public string DownloadIdentifier { get; set; }
        public string Identifier { get; }
        public event Action StateChanged;
        public RepositoryModState GetState()
        {
            throw new NotImplementedException();
        }

        public void ChangeAction(IModelStorageMod target, ModActionEnum? newAction)
        {
            throw new NotImplementedException();
        }

        public event Action<IModelStorageMod> ActionAdded;
        public event Action SelectionChanged;
        public event Action DownloadIdentifierChanged;

        public void DoUpdate()
        {
            throw new NotImplementedException();
        }

        IUpdateState IModelRepositoryMod.CurrentUpdate => _currentUpdate;

        public event Action OnUpdateChange;
    }
}
