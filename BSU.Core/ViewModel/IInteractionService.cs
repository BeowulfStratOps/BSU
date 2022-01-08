namespace BSU.Core.ViewModel
{
    public interface IInteractionService
    {
        bool AddRepository(AddRepository viewModel);

        bool AddStorage(AddStorage viewModel);

        void MessagePopup(string message, string title);

        bool? YesNoCancelPopup(string message, string title);

        bool YesNoPopup(string message, string title);
        bool SelectRepositoryStorage(SelectRepositoryStorage viewModel);
        bool PresetSettings(PresetSettings vm);
    }
}
