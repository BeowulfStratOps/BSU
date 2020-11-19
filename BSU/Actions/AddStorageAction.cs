using System;
using System.Threading.Tasks;
using BSU.Core.ViewModel;
using BSU.GUI.Dialogs;

namespace BSU.GUI.Actions
{
    public class AddStorageAction : Interaction<AddStorage, bool?>
    {
        protected override void Invoke(AddStorage viewModel, TaskCompletionSource<bool?> tcs)
        {
            var result = new AddStorageDialog(viewModel).ShowDialog();
            tcs.SetResult(result);
        }
    }
}
