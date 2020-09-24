using System;
using BSU.Core.ViewModel;
using BSU.GUI.Dialogs;

namespace BSU.GUI.Actions
{
    public class AddStorageAction : Interaction<AddStorage, bool?>
    {
        protected override void Invoke(AddStorage viewModel, Action<bool?> callback)
        {
            var result = new AddStorageDialog(viewModel).ShowDialog();
            callback(result);
        }
    }
}
