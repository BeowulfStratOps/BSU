using System;
using System.Threading;

namespace BSU.Core.Concurrency
{
    public class SynchronizationContextEventBus : IEventBus
    {
        private readonly SynchronizationContext _synchronizationContext;

        public SynchronizationContextEventBus(SynchronizationContext synchronizationContext)
        {
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException();
        }

        public void ExecuteSynchronized(Action action)
        {
            _synchronizationContext.Send(_ => action(), null);
        }
    }

    public interface IEventBus
    {
        void ExecuteSynchronized(Action action);
    }
}
