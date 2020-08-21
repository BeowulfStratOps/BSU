using System;

namespace BSU.Core.ViewModel
{
    public class YesNoInteractionRequest
    {
        public event EventHandler<YesNoInteractionRequestsEventArgs> Raised;

        public void Raise(YesNoPopupContext context, Action<bool> callback)
        {
            Raised?.Invoke(context, new YesNoInteractionRequestsEventArgs(context, callback));
        }
    }

    public class YesNoInteractionRequestsEventArgs : EventArgs
    {
        public YesNoPopupContext Context { get; }
        public Action<bool> Callback { get; }

        public YesNoInteractionRequestsEventArgs(YesNoPopupContext context, Action<bool> callback)
        {
            Context = context;
            Callback = callback;
        }
    }

    public class YesNoPopupContext
    {
        public string Title { get; }
        public string Message { get; }

        public YesNoPopupContext(string message, string title)
        {
            Message = message;
            Title = title;
        }
    }
}
