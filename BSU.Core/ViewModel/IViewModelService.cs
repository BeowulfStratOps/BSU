using BSU.Core.Model;

namespace BSU.Core.ViewModel
{
    internal interface IViewModelService : INavigator
    {
        IModelStorage? AddStorage();
    }
}
