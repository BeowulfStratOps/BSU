using System;
using System.Threading.Tasks;
using BSU.Core.ViewModel;
using BSU.GUI.Dialogs;

namespace BSU.GUI.Actions
{
    public class AddRepositoryAction : Interaction<AddRepository, bool?>
    {
        protected override void Invoke(AddRepository viewModel, TaskCompletionSource<bool?> tcs)
        {
            var result = new AddRepositoryDialog(viewModel).ShowDialog();
            tcs.SetResult(result);
        }
    }
}
