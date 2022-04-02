using System;
using System.Collections.Generic;
using System.Linq;
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
            return (bool)new AddRepositoryDialog(viewModel).ShowDialog()!;
        }

        public bool AddStorage(AddStorage viewModel)
        {
            return (bool)new AddStorageDialog(viewModel).ShowDialog()!;
        }

        public void MessagePopup(string message, string title, MessageImageEnum image)
        {
            new MessageDialog(message, title, image).ShowDialog();
        }

        public T OptionsPopup<T>(string message, string title, Dictionary<T, string> options, MessageImageEnum image) where T : notnull
        {
            var dialog = new OptionsDialog(message, title, options.ToDictionary(kv => (object)kv.Key, kv => kv.Value),
                image);
            var result = dialog.ShowDialog();
            if (result != true)
                return default!;
            return (T)dialog.Result!;
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
