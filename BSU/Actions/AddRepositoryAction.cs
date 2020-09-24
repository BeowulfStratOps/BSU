using System;
using BSU.Core.ViewModel;
using BSU.GUI.Dialogs;

namespace BSU.GUI.Actions
{
    public class AddRepositoryAction : Interaction<AddRepository, bool?>
    {
        protected override void Invoke(AddRepository viewModel, Action<bool?> callback)
        {
            var result = new AddRepositoryDialog(viewModel).ShowDialog();
            callback(result);
        }
    }
}
