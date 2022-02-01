using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Launch;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal interface IModel
    {
        // TODO: thin out, split into different interfaces

        void DeleteRepository(IModelRepository repository, bool removeMods);
        void DeleteStorage(IModelStorage storage, bool removeMods);
        IModelRepository AddRepository(string type, string url, string name, PresetSettings settings);
        IModelStorage AddStorage(string type, string path, string name);
        IEnumerable<IModelStorage> GetStorages();
        IEnumerable<IModelRepository> GetRepositories();
        Task<ServerInfo?> CheckRepositoryUrl(string url, CancellationToken cancellationToken);

        event Action<IModelRepository> AddedRepository;
        event Action<IModelStorage> AddedStorage;
        List<IModelStorageMod> GetStorageMods();
        List<IModelRepositoryMod> GetRepositoryMods();
        event Action<IModelRepository> RemovedRepository;
        event Action<IModelStorage> RemovedStorage;
    }
}
