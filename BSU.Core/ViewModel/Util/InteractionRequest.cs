using System;
using System.Threading.Tasks;

namespace BSU.Core.ViewModel.Util
{
    public class InteractionRequest<TViewModel, TResult>
    {
        public event EventHandler<InteractionRequestArgs<TViewModel, TResult>> Raised;

        public Task<TResult> Raise(TViewModel viewModel)
        {
            var tcs = new TaskCompletionSource<TResult>();
            Raised?.Invoke(viewModel, new InteractionRequestArgs<TViewModel, TResult>(viewModel, tcs));
            return tcs.Task;
        }
    }
    
    public class InteractionRequestArgs<TViewModel, TResult> : EventArgs
    {
        public TViewModel ViewModel { get; }
        public TaskCompletionSource<TResult> TaskCompletionSource { get; }

        public InteractionRequestArgs(TViewModel viewModel, TaskCompletionSource<TResult> taskCompletionSource)
        {
            ViewModel = viewModel;
            TaskCompletionSource = taskCompletionSource;
        }
    }
}