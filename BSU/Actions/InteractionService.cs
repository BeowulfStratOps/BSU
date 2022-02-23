using System;
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

        private static MessageBoxImage MessageImageToMessageBoxImage(MessageImage image)
        {
            return image switch
            {
                MessageImage.Question => MessageBoxImage.Question,
                MessageImage.Error => MessageBoxImage.Error,
                MessageImage.Warning => MessageBoxImage.Warning,
                MessageImage.Success => MessageBoxImage.None,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public bool AddRepository(AddRepository viewModel)
        {
            return (bool)new AddRepositoryDialog(viewModel).ShowDialog()!;
        }

        public bool AddStorage(AddStorage viewModel)
        {
            return (bool)new AddStorageDialog(viewModel).ShowDialog()!;
        }

        public void MessagePopup(string message, string title, MessageImage image)
        {
            MessageBox.Show(_owner, message, title, MessageBoxButton.OK, MessageImageToMessageBoxImage(image));
        }

        public bool? YesNoCancelPopup(string message, string title, MessageImage image)
        {
            var q = MessageBox.Show(_owner, message, title, MessageBoxButton.YesNoCancel, MessageImageToMessageBoxImage(image));
            if (q == MessageBoxResult.Cancel)
            {
                return null;
            }
            return q == MessageBoxResult.Yes;
        }

        public bool YesNoPopup(string message, string title, MessageImage image)
        {
            var q = MessageBox.Show(_owner, message, title, MessageBoxButton.YesNo, MessageImageToMessageBoxImage(image));
            return q == MessageBoxResult.Yes;
        }

        public bool SelectRepositoryStorage(SelectRepositoryStorage viewModel)
        {
            return (bool)new SelectRepositoryStorageDialog(viewModel).ShowDialog()!;
        }

        public bool GlobalSettings(GlobalSettings vm)
        {
            return (bool)new GlobalSettingsDialog(vm).ShowDialog()!;
        }

        public void CloseBsu()
        {
            _owner.Close();
        }
    }
}
