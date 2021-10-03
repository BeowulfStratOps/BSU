using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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
    }
}
