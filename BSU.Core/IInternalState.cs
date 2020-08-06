using System;
using System.Collections.Generic;
using System.IO;
using BSU.Core.JobManager;
using BSU.Core.Model;

namespace BSU.Core
{
    internal interface IInternalState
    {
        IReadOnlyList<StorageEntry> GetStorages();
        IReadOnlyList<RepositoryEntry> GetRepositories();
        void RemoveRepo(Repository repo);
        void AddRepo(string name, string url, string type);
        void AddStorage(string name, DirectoryInfo directory, string type);
        void RemoveStorage(Model.Storage storage);
        void SetUpdatingTo(StorageMod mod, string targetHash, string targetDisplay);
        void RemoveUpdatingTo(StorageMod mod);
        void CleanupUpdatingTo(Model.Storage storage);
        UpdateTarget GetUpdateTarget(StorageMod mod);
        void SetUsedMod(RepositoryMod repositoryMod, StorageMod storageMod);
        public bool IsUsedMod(RepositoryMod repositoryMod, StorageMod storageMod);
        public bool HasUsedMod(RepositoryMod repositoryMod);
    }
}