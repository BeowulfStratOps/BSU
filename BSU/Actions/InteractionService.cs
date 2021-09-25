using System.Windows;
using BSU.Core.ViewModel;
using BSU.GUI.Dialogs;

namespace BSU.GUI.Actions
{
    public class InteractionService : IInteractionService
    {
        public bool? AddRepository(AddRepository viewModel)
        {
            return new AddRepositoryDialog(viewModel).ShowDialog();
        }

        public bool? AddStorage(AddStorage viewModel)
        {
            return new AddStorageDialog(viewModel).ShowDialog();
        }

        public void MessagePopup(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK);
        }

        public bool? YesNoCancelPopup(string message, string title)
        {
            var q = MessageBox.Show(message, title, MessageBoxButton.YesNoCancel);
            if (q == MessageBoxResult.Cancel)
            {
                return null;
            }
            return q == MessageBoxResult.Yes;
        }

        public bool YesNoPopup(string message, string title)
        {
            var q = MessageBox.Show(message, title, MessageBoxButton.YesNo);
            return q == MessageBoxResult.Yes;
        }
    }
}
