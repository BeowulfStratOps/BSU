using BSU.Core.Model;

namespace BSU.Core.ViewModel
{
    internal interface IViewModelService
    {
        void NavigateToRepository(Repository repository);
        IModelStorage? AddStorage();
        Repository FindVmRepo(IModelRepository repo);
    }
}
