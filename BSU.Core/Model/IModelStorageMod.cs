﻿using System;
using BSU.Core.Persistence;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal interface IModelStorageMod
    {
        void RequireHash();
        event Action StateChanged;
        IUpdateState PrepareUpdate(IModelRepositoryMod repositoryMod, Action rollback = null);
        StorageModState GetState();
        void Abort();
        StorageModIdentifiers GetStorageModIdentifiers();
        bool CanWrite { get; }
    }
}