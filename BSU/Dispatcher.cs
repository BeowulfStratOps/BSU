using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using BSU.Core.Model;

namespace BSU.GUI
{
    public class CoreDispatcher : IActionQueue
    {
        private readonly Dispatcher _dispatcher;

        public CoreDispatcher(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void EnQueueAction(Action action)
        {
            _dispatcher.BeginInvoke(DispatcherPriority.Background, action);
        }
    }
}
