using System;
using System.Threading.Tasks;
using System.Windows;
using BSU.Core.ViewModel;

namespace BSU.GUI.Actions
{
    public class YesNoCancelPopupAction : Interaction<MsgPopupContext, bool?>
    {
        protected override void Invoke(MsgPopupContext context, TaskCompletionSource<bool?> tcs)
        {
            var q = MessageBox.Show(context.Message, context.Title, MessageBoxButton.YesNoCancel);
            if (q == MessageBoxResult.Cancel)
            {
                tcs.SetResult(null);
                return;
            }
            tcs.SetResult(q == MessageBoxResult.Yes);
        }
    }
}
