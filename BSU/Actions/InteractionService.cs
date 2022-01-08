using System.Windows;
using BSU.Core.ViewModel;
using BSU.GUI.Dialogs;

namespace BSU.GUI.Actions
{
    public class InteractionService : IInteractionService
    {
        private readonly Window _owner;

        public InteractionService(Window owner)
        {
            _owner = owner;
        }

        public bool AddRepository(AddRepository viewModel)
        {
            return (bool)new AddRepositoryDialog(viewModel).ShowDialog();
        }

        public bool AddStorage(AddStorage viewModel)
        {
            return (bool)new AddStorageDialog(viewModel).ShowDialog();
        }

        public void MessagePopup(string message, string title)
        {
            MessageBox.Show(_owner, message, title, MessageBoxButton.OK);
        }

        public bool? YesNoCancelPopup(string message, string title)
        {
            var q = MessageBox.Show(_owner, message, title, MessageBoxButton.YesNoCancel);
            if (q == MessageBoxResult.Cancel)
            {
                return null;
            }
            return q == MessageBoxResult.Yes;
        }

        public bool YesNoPopup(string message, string title)
        {
            var q = MessageBox.Show(_owner, message, title, MessageBoxButton.YesNo);
            return q == MessageBoxResult.Yes;
        }

        public bool SelectRepositoryStorage(SelectRepositoryStorage viewModel)
        {
            return (bool)new SelectRepositoryStorageDialog(viewModel).ShowDialog();
        }

        public bool PresetSettings(PresetSettings vm)
        {
            return (bool)new PresetSettingsDialog(vm).ShowDialog();
        }
    }
}
