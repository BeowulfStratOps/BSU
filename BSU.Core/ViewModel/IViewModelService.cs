using System.Threading.Tasks;

namespace BSU.Core.ViewModel
{
    public interface IViewModelService
    {
        Task Update();
        void NavigateToRepositories();
        void NavigateToStorages();
        void NavigateToRepository(Repository repository);
        void NavigateBack();
        IInteractionService InteractionService { get; }
    }
}
