using System;
using System.Threading.Tasks;
using System.Windows;
using BSU.Core.ViewModel;

namespace BSU.GUI.Actions
{
    public class MsgPopupAction : Interaction<MsgPopupContext, object>
    {
        protected override void Invoke(MsgPopupContext context, TaskCompletionSource<object> tcs)
        {
           MessageBox.Show(context.Message, context.Title, MessageBoxButton.OK);
           tcs.SetResult(null);
        }
    }
}
