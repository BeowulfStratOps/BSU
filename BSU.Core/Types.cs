using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.BSO;
using BSU.Core.Storage;
using BSU.CoreCommon;

namespace BSU.Core
{
    public class Types
    {
        public static Types Default
        {
            get
            {
                var types = new Types();
                types.AddRepoType(BsoRepo.RepoType, (url, jobManager) => new BsoRepo(url, jobManager));
                types.AddStorageType("STEAM", (_, jobManager) => new SteamStorage(jobManager));
                types.AddStorageType("DIRECTORY", (path, jobManager) => new DirectoryStorage(path, jobManager));
                return types;
            }
        }

        private readonly Dictionary<string, Func<string, IJobManager, IRepository>> _repoTypes = new();

        private readonly Dictionary<string, Func<string, IJobManager, IStorage>> _storageTypes = new();

        public void AddRepoType(string name, Func<string, IJobManager, IRepository> create) =>
            _repoTypes.Add(name, create);

        public IEnumerable<string> GetRepoTypes() => _repoTypes.Keys.ToList();

        public void AddStorageType(string name, Func<string, IJobManager, IStorage> create) =>
            _storageTypes.Add(name, create);

        public IEnumerable<string> GetStorageTypes() => _storageTypes.Keys.ToList();

        internal IRepository GetRepoImplementation(string repoType, string repoUrl, IJobManager jobManager)
        {
            if (!_repoTypes.TryGetValue(repoType, out var create))
                throw new NotSupportedException($"Repo type {repoType} is not supported.");
            return create(repoUrl, jobManager);
        }

        internal IStorage GetStorageImplementation(string storageType, string path, IJobManager jobManager)
        {
            if (!_storageTypes.TryGetValue(storageType, out var create))
                throw new NotSupportedException($"Storage type {storageType} is not supported.");
            return create(path, jobManager);
        }

        public async Task<ServerUrlCheck?> CheckUrl(string url, CancellationToken cancellationToken)
        {
            return await BsoRepo.CheckUrl(url, cancellationToken);
        }
    }
}
