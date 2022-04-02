using System;
using System.Collections.Generic;

namespace BSU.Core.ViewModel
{
    public interface IInteractionService
    {
        bool AddRepository(AddRepository viewModel);

        bool AddStorage(AddStorage viewModel);

        void MessagePopup(string message, string title, MessageImageEnum image);

        T OptionsPopup<T>(string message, string title, Dictionary<T, string> options, MessageImageEnum image) where T : notnull;
        bool SelectRepositoryStorage(SelectRepositoryStorage viewModel);
        bool GlobalSettings(GlobalSettings vm);
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
