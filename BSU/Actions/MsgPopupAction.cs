using System;
using System.Windows;
using BSU.Core.ViewModel;

namespace BSU.GUI.Actions
{
    public class MsgPopupAction : Interaction<MsgPopupContext, object>
    {
        protected override void Invoke(MsgPopupContext context, Action<object> callback)
        {
           MessageBox.Show(context.Message, context.Title, MessageBoxButton.OK);
        }
    }
}
