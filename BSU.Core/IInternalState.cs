using System;
using System.Collections.Generic;
using System.IO;
using BSU.Core.JobManager;
using BSU.Core.Model;

namespace BSU.Core
{
    internal interface IInternalState
    {
        IReadOnlyList<Tuple<RepoEntry, Exception>> GetRepoErrors();
        IReadOnlyList<Tuple<StorageEntry, Exception>> GetStorageErrors();
        List<Model.Storage> LoadStorages(IJobManager jobManager);
        List<Repository> LoadRepositories(IJobManager jobManager);
        void RemoveRepo(Repository repo);
        Repository AddRepo(string name, string url, string type, Model.Model model, IJobManager jobManager);
        Repository LoadRepository(RepoEntry repo, IJobManager jobManager);
        Model.Storage AddStorage(string name, DirectoryInfo directory, string type, IJobManager jobManager);
        void RemoveStorage(Model.Storage storage);
        Model.Storage LoadStorage(StorageEntry storage, IJobManager jobManager);
        void SetUpdatingTo(StorageMod mod, string targetHash, string targetDisplay);
        void RemoveUpdatingTo(StorageMod mod);
        void CleanupUpdatingTo(Model.Storage storage);
        UpdateTarget GetUpdateTarget(StorageMod mod);
    }
}