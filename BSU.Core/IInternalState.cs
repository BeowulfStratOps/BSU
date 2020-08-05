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
        List<Model.Storage> LoadStorages(IJobManager jobManager, MatchMaker matchMaker, Model.Model model);
        List<Repository> LoadRepositories(IJobManager jobManager, MatchMaker matchMaker, Model.Model model);
        void RemoveRepo(Repository repo);
        Repository AddRepo(string name, string url, string type, Model.Model model, IJobManager jobManager, MatchMaker matchMaker);
        Repository LoadRepository(RepoEntry repo, IJobManager jobManager, MatchMaker matchMaker, Model.Model model);
        Model.Storage AddStorage(string name, DirectoryInfo directory, string type, IJobManager jobManager, MatchMaker matchMaker, Model.Model model);
        void RemoveStorage(Model.Storage storage);
        Model.Storage LoadStorage(StorageEntry storage, IJobManager jobManager, MatchMaker matchMaker, Model.Model model);
        void SetUpdatingTo(StorageMod mod, string targetHash, string targetDisplay);
        void RemoveUpdatingTo(StorageMod mod);
        void CleanupUpdatingTo(Model.Storage storage);
        UpdateTarget GetUpdateTarget(StorageMod mod);
        void SetUsedMod(RepositoryMod repositoryMod, StorageMod storageMod);

        public bool IsUsedMod(RepositoryMod repositoryMod, StorageMod storageMod);
        public bool HasUsedMod(RepositoryMod repositoryMod);
    }
}