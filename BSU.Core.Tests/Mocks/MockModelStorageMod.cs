﻿using System;
using BSU.Core.Model;
using BSU.Core.Persistence;
using BSU.CoreCommon;
using StorageModState = BSU.Core.Model.StorageModState;

namespace BSU.Core.Tests.Mocks
{
    internal class MockModelStorageMod : IModelStorageMod
    {
        public void RequireHash()
        {
            throw new NotImplementedException();
        }

        public event Action StateChanged;

        public IUpdateState PrepareUpdate(IRepositoryMod repositoryMod, UpdateTarget target, Action<Exception> setupError,
            Action rollback = null)
        {
            throw new NotImplementedException();
        }

        public IUpdateState PrepareUpdate(IRepositoryMod repositoryMod, UpdateTarget target)
        {
            throw new NotImplementedException();
        }

        public StorageModState GetState()
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
        public IStorageMod Implementation { get; }
    }
}
