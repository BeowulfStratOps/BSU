using System;

namespace BSU.Core.ViewModel.Util
{
    public class InteractionRequest<TViewModel, TCallback>
    {
        public event EventHandler<InteractionRequestArgs<TViewModel, TCallback>> Raised;

        public void Raise(TViewModel viewModel, Action<TCallback> callback)
        {
            Raised?.Invoke(viewModel, new InteractionRequestArgs<TViewModel, TCallback>(viewModel, callback));
        }
    }
    
    public class InteractionRequestArgs<TViewModel, TCallback> : EventArgs
    {
        public TViewModel ViewModel { get; }
        public Action<TCallback> Callback { get; }

        public InteractionRequestArgs(TViewModel viewModel, Action<TCallback> callback)
        {
            ViewModel = viewModel;
            Callback = callback;
        }
    }
}