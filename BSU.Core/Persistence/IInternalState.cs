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
        (IRepositoryEntry entry, IRepositoryState state) AddRepo(string name, string url, string type);
        void RemoveStorage(Guid storageIdentifier);
        (IStorageEntry entry, IStorageState state) AddStorage(string name, string path, string type);
    }
}
