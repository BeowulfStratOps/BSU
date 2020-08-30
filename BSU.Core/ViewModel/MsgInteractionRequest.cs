using System;

namespace BSU.Core.ViewModel
{
    public class MsgInteractionRequest
    {
        public event EventHandler<MsgInteractionRequestsEventArgs> Raised;

        public void Raise(MsgPopupContext context)
        {
            Raised?.Invoke(context, new MsgInteractionRequestsEventArgs(context));
        }
    }

    public class MsgInteractionRequestsEventArgs : EventArgs
    {
        public MsgPopupContext Context { get; }

        public MsgInteractionRequestsEventArgs(MsgPopupContext context)
        {
            Context = context;
        }
    }

    public class MsgPopupContext
    {
        public string Title { get; }
        public string Message { get; }

        public MsgPopupContext(string message, string title)
        {
            Message = message;
            Title = title;
        }
    }
}
