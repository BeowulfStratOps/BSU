namespace BSU.Core.ViewModel
{
    public interface IInteractionService
    {
        bool AddRepository(AddRepository viewModel);

        bool AddStorage(AddStorage viewModel);

        void MessagePopup(string message, string title, MessageImage image);

        bool? YesNoCancelPopup(string message, string title, MessageImage image);

        bool YesNoPopup(string message, string title, MessageImage image);
        bool SelectRepositoryStorage(SelectRepositoryStorage viewModel);
        bool GlobalSettings(GlobalSettings vm);
        void CloseBsu();
    }

    public enum MessageImage
    {
        Question,
        Error,
        Warning,
        Success
    }
}
