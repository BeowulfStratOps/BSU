using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal interface IModel
    {
        void DeleteRepository(IModelRepository repository, bool removeMods);
        void DeleteStorage(IModelStorage storage, bool removeMods);
        IModelRepository AddRepository(string type, string url, string name);
        IModelStorage AddStorage(string type, DirectoryInfo path, string name);
        IEnumerable<IModelStorage> GetStorages();
        IEnumerable<IModelRepository> GetRepositories();
        void ConnectErrorPresenter(IErrorPresenter presenter);
        Task<ServerInfo> CheckRepositoryUrl(string url, CancellationToken cancellationToken);

        public event Action<IModelRepository> AddedRepository;
        public event Action<IModelStorage> AddedStorage;
        List<IModelStorageMod> GetStorageMods();
        List<IModelRepositoryMod> GetRepositoryMods();
    }
}
