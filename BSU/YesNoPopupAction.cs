using System;
using System.Windows;
using BSU.Core.ViewModel;
using Microsoft.Xaml.Behaviors;

namespace BSU.GUI
{
    public class YesNoPopupAction : TriggerAction<DependencyObject>
    {
        protected override void Invoke(object parameter)
        {
            if (!(parameter is YesNoInteractionRequestsEventArgs args)) throw new ArgumentException();
            var q = MessageBox.Show(args.Context.Message, args.Context.Title, MessageBoxButton.YesNo);
            args.Callback(q == MessageBoxResult.Yes);
        }
    }
}
