using System;
using System.Collections.Generic;
using System.IO;
using BSU.Core.Model;

namespace BSU.Core.Persistence
{
    internal interface IInternalState
    {
        IReadOnlyList<Tuple<StorageEntry, IStorageState>> GetStorages();
        IReadOnlyList<RepositoryEntry> GetRepositories();
        void RemoveRepo(Repository repo);
        void AddRepo(string name, string url, string type);
        void RemoveStorage(StorageEntry storage);
        IStorageState AddStorage(string name, DirectoryInfo directory, string type);
        void SetUsedMod(RepositoryMod repositoryMod, StorageMod storageMod);
        public bool IsUsedMod(RepositoryMod repositoryMod, StorageMod storageMod);
        public bool HasUsedMod(RepositoryMod repositoryMod);
    }
}