using System.Collections.Generic;
using System.Threading.Tasks;

namespace BSU.Core.ViewModel
{
    public interface IInteractionService
    {
        Task<bool> AddRepository(AddRepository viewModel);

        Task<bool> AddStorage(AddStorage viewModel);

        Task MessagePopup(string message, string title, MessageImageEnum image);

        Task<T> OptionsPopup<T>(string message, string title, Dictionary<T, string> options, MessageImageEnum image) where T : notnull;
        Task<bool> SelectRepositoryStorage(SelectRepositoryStorage viewModel);
        Task<bool> GlobalSettings(GlobalSettings vm);
        void CloseBsu();
    }

    public enum MessageImageEnum
    {
        Question,
        Error,
        Warning,
        Success
    }
}
