using System;
using BSU.Core.Model;

namespace BSU.Core
{
    public class Dispatcher : IActionQueue
    {
        private readonly Action<Action> _dispatch;

        public Dispatcher(Action<Action> dispatch)
        {
            _dispatch = dispatch;
        }
        
        public void EnQueueAction(Action action)
        {
            _dispatch(action);
        }
    }
}