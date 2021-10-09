using System.Threading.Tasks;
using BSU.Core.Model;

namespace BSU.Core.ViewModel
{
    internal interface IViewModelService
    {
        Task Update();
        void NavigateToStorages();
        void NavigateToRepository(Repository repository);
        void NavigateBack();
        IInteractionService InteractionService { get; }
        IModelStorage AddStorage(bool allowSteam);
    }
}
