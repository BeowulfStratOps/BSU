using System.Threading.Tasks;
using BSU.Core.Model;

namespace BSU.Core.ViewModel
{
    internal interface IViewModelService
    {
        void NavigateToStorages();
        void NavigateToRepository(Repository repository);
        void NavigateBack();
        IInteractionService InteractionService { get; }
        IModelStorage AddStorage(bool allowSteam);
        Repository FindVmRepo(IModelRepository repo);
    }
}
