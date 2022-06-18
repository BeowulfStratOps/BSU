using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public Task<bool> AddRepository(AddRepository viewModel)
        {
            return Task.FromResult((bool)new AddRepositoryDialog(viewModel).ShowDialog()!);
        }

        public Task<bool> AddStorage(AddStorage viewModel)
        {
            return Task.FromResult((bool)new AddStorageDialog(viewModel).ShowDialog()!);
        }

        public Task MessagePopup(string message, string title, MessageImageEnum image)
        {
            new MessageDialog(message, title, image).ShowDialog();
            return Task.CompletedTask;
        }

        public Task<T> OptionsPopup<T>(string message, string title, Dictionary<T, string> options, MessageImageEnum image) where T : notnull
        {
            var dialog = new OptionsDialog(message, title, options.ToDictionary(kv => (object)kv.Key, kv => kv.Value),
                image);
            var result = dialog.ShowDialog();
            return result != true ? Task.FromResult<T>(default!) : Task.FromResult((T)dialog.Result!);
        }

        public Task<bool> SelectRepositoryStorage(SelectRepositoryStorage viewModel)
        {
            return Task.FromResult((bool)new SelectRepositoryStorageDialog(viewModel).ShowDialog()!);
        }

        public Task<bool> GlobalSettings(GlobalSettings vm)
        {
            return Task.FromResult((bool)new GlobalSettingsDialog(vm).ShowDialog()!);
        }

        public void CloseBsu()
        {
            _owner.Close();
        }
    }
}
