using System;
using System.Threading.Tasks;
using System.Windows;
using BSU.Core.ViewModel.Util;
using Microsoft.Xaml.Behaviors;

namespace BSU.GUI.Actions
{
    public abstract class Interaction<TViewModel, TResult> : TriggerAction<DependencyObject>
    {
        protected sealed override void Invoke(object parameter)
        {
            if (!(parameter is InteractionRequestArgs<TViewModel, TResult> args)) throw new ArgumentException();
            Invoke(args.ViewModel, args.TaskCompletionSource);
        }

        protected abstract void Invoke(TViewModel viewModel, TaskCompletionSource<TResult> callback);
    }
}