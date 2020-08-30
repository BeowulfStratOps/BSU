using System;
using System.Windows;
using BSU.Core.ViewModel;
using Microsoft.Xaml.Behaviors;

namespace BSU.GUI
{
    public class MsgPopupAction : TriggerAction<DependencyObject>
    {
        protected override void Invoke(object parameter)
        {
            if (!(parameter is MsgInteractionRequestsEventArgs args)) throw new ArgumentException();
            var q = MessageBox.Show(args.Context.Message, args.Context.Title, MessageBoxButton.OK);
        }
    }
}
