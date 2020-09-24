using System;
using System.Windows;
using BSU.Core.ViewModel.Util;
using Microsoft.Xaml.Behaviors;

namespace BSU.GUI.Actions
{
    public abstract class Interaction<TViewModel, TCallback> : TriggerAction<DependencyObject>
    {
        protected sealed override void Invoke(object parameter)
        {
            if (!(parameter is InteractionRequestArgs<TViewModel, TCallback> args)) throw new ArgumentException();
            Invoke(args.ViewModel, args.Callback);
        }

        protected abstract void Invoke(TViewModel viewModel, Action<TCallback> callback);
    }
}