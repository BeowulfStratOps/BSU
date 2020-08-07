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
        void RemoveRepo(IRepositoryEntry repo);
        IRepositoryState AddRepo(string name, string url, string type);
        void RemoveStorage(IStorageEntry storage);
        IStorageState AddStorage(string name, DirectoryInfo directory, string type);
    }
}