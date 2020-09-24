using System;
using System.Windows;
using BSU.Core.ViewModel;

namespace BSU.GUI.Actions
{
    public class YesNoPopupAction : Interaction<MsgPopupContext, bool>
    {
        protected override void Invoke(MsgPopupContext context, Action<bool> callback)
        {
            var q = MessageBox.Show(context.Message, context.Title, MessageBoxButton.YesNo);
            callback(q == MessageBoxResult.Yes);
        }
    }
}
