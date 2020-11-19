using System;
using System.Threading.Tasks;
using System.Windows;
using BSU.Core.ViewModel;

namespace BSU.GUI.Actions
{
    public class YesNoPopupAction : Interaction<MsgPopupContext, bool>
    {
        protected override void Invoke(MsgPopupContext context, TaskCompletionSource<bool> tcs)
        {
            var q = MessageBox.Show(context.Message, context.Title, MessageBoxButton.YesNo);
            tcs.SetResult(q == MessageBoxResult.Yes);
        }
    }
}
