using System;
using System.Collections.Generic;
using System.Linq;
using BSU.BSO;
using BSU.Core.Storage;
using BSU.CoreCommon;

namespace BSU.Core
{
    public class Types
    {
        private readonly Dictionary<string, Func<string, IRepository>> _repoTypes =
            new Dictionary<string, Func<string, IRepository>>
            {
                {"BSO", url => new BsoRepo(url)}
            };

        private readonly Dictionary<string, Func<string, IStorage>> _storageTypes =
            new Dictionary<string, Func<string, IStorage>>
            {
                {"STEAM", path => new SteamStorage(path)},
                {"DIRECTORY", path => new DirectoryStorage(path)}
            };

        public void AddRepoType(string name, Func<string, IRepository> create) =>
            _repoTypes.Add(name, create);

        public IEnumerable<string> GetRepoTypes() => _repoTypes.Keys.ToList();

        public void AddStorageType(string name, Func<string, IStorage> create) =>
            _storageTypes.Add(name, create);

        public IEnumerable<string> GetStorageTypes() => _storageTypes.Keys.ToList();

        internal IRepository GetRepoImplementation(string repoType, string repoUrl)
        {
            if (!_repoTypes.TryGetValue(repoType, out var create))
                throw new NotSupportedException($"Repo type {repoType} is not supported.");
            return create(repoUrl);
        }
        
        internal IStorage GetStorageImplementation(string stroageType, string path)
        {
            if (!_storageTypes.TryGetValue(stroageType, out var create))
                throw new NotSupportedException($"Repo type {stroageType} is not supported.");
            return create(path);
        }
    }
}