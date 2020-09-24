using System;
using System.Windows;
using BSU.Core.ViewModel;

namespace BSU.GUI.Actions
{
    public class YesNoCancelPopupAction : Interaction<MsgPopupContext, bool?>
    {
        protected override void Invoke(MsgPopupContext context, Action<bool?> callback)
        {
            var q = MessageBox.Show(context.Message, context.Title, MessageBoxButton.YesNoCancel);
            if (q == MessageBoxResult.Cancel)
            {
                callback(null);
                return;
            }
            callback(q == MessageBoxResult.Yes);
        }
    }
}
