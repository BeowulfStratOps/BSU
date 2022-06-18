using System.Threading.Tasks;
using BSU.Core.Model;

namespace BSU.Core.ViewModel
{
    internal interface IViewModelService : INavigator
    {
        Task<IModelStorage?> AddStorage();
    }
}
