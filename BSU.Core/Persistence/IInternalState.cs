using System;
using System.Collections.Generic;
using System.IO;
using BSU.Core.Model;

namespace BSU.Core.Persistence
{
    internal interface IInternalState
    {
        IEnumerable<Tuple<IStorageEntry, IStorageState>> GetStorages();
        IEnumerable<Tuple<IRepositoryEntry, IRepositoryState>> GetRepositories();
        void RemoveRepository(Guid repositoryIdentifier);
        IRepositoryState AddRepo(string name, string url, string type);
        void RemoveStorage(Guid storageIdentifier);
        IStorageState AddStorage(string name, DirectoryInfo directory, string type);
    }
}