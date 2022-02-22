using BSU.Core.Model;

namespace BSU.Core.ViewModel
{
    internal interface IViewModelService
    {
        void NavigateToStorages();
        void NavigateToRepository(Repository repository);
        void NavigateBack();
        IModelStorage? AddStorage();
        Repository FindVmRepo(IModelRepository repo);
    }
}
