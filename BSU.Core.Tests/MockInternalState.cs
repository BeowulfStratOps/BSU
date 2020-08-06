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

        public IReadOnlyList<StorageEntry> GetStorages()
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<RepositoryEntry> GetRepositories()
        {
            throw new NotImplementedException();
        }

        public void RemoveRepo(Repository repo)
        {
            throw new NotImplementedException();
        }

        public void AddRepo(string name, string url, string type)
        {
            throw new NotImplementedException();
        }

        public void AddStorage(string name, DirectoryInfo directory, string type)
        {
            throw new NotImplementedException();
        }

        public void RemoveStorage(Model.Storage storage)
        {
            throw new NotImplementedException();
        }

        public void SetUpdatingTo(StorageMod mod, string targetHash, string targetDisplay)
        {
            _updateTargets[mod] = new UpdateTarget(targetHash, targetDisplay);
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

        public void SetUsedMod(RepositoryMod repositoryMod, StorageMod storageMod)
        {
            throw new NotImplementedException();
        }

        public bool IsUsedMod(RepositoryMod repositoryMod, StorageMod storageMod)
        {
            throw new NotImplementedException();
        }

        public bool HasUsedMod(RepositoryMod repositoryMod)
        {
            throw new NotImplementedException();
        }
    }
}