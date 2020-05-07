using System;
using System.Collections.Generic;
using System.IO;
using BSU.Core.JobManager;
using BSU.Core.Model;

namespace BSU.Core.Tests
{
    internal class MockInternalState : IInternalState
    {
        private readonly Dictionary<StorageMod, UpdateTarget> _updateTargets = new Dictionary<StorageMod, UpdateTarget>();
        public UpdateTarget MockUpdatingTo { get; set; }

        public IReadOnlyList<Tuple<RepoEntry, Exception>> GetRepoErrors()
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<Tuple<StorageEntry, Exception>> GetStorageErrors()
        {
            throw new NotImplementedException();
        }

        public List<Model.Storage> LoadStorages(IJobManager jobManager)
        {
            throw new NotImplementedException();
        }

        public List<Repository> LoadRepositories(IJobManager jobManager)
        {
            throw new NotImplementedException();
        }

        public void RemoveRepo(Repository repo)
        {
            throw new NotImplementedException();
        }

        public Repository AddRepo(string name, string url, string type, Model.Model model, IJobManager jobManager)
        {
            throw new NotImplementedException();
        }

        public Repository LoadRepository(RepoEntry repo, IJobManager jobManager)
        {
            throw new NotImplementedException();
        }

        public Model.Storage AddStorage(string name, DirectoryInfo directory, string type, IJobManager jobManager)
        {
            throw new NotImplementedException();
        }

        public void RemoveStorage(Model.Storage storage)
        {
            throw new NotImplementedException();
        }

        public Model.Storage LoadStorage(StorageEntry storage, IJobManager jobManager)
        {
            throw new NotImplementedException();
        }

        public void SetUpdatingTo(StorageMod mod, string targetHash, string targetDisplay)
        {
            _updateTargets.Add(mod, new UpdateTarget(targetHash, targetDisplay));
        }

        public void RemoveUpdatingTo(StorageMod mod)
        {
            _updateTargets.Remove(mod);
        }

        public void CleanupUpdatingTo(Model.Storage storage)
        {
            throw new NotImplementedException();
        }

        public UpdateTarget GetUpdateTarget(StorageMod mod)
        {
            if (_updateTargets.TryGetValue(mod, out var target)) return target;
            if (MockUpdatingTo == null) return null;
            _updateTargets[mod] = MockUpdatingTo;
            return MockUpdatingTo;
        }
    }
}