using System;

namespace BSU.Core.ViewModel
{
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
