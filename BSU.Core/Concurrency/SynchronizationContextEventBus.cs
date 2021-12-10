using System;
using System.Threading;
using NLog;

namespace BSU.Core.Concurrency
{
    public class SynchronizationContextEventBus : IEventBus
    {
        private readonly SynchronizationContext _synchronizationContext;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public SynchronizationContextEventBus(SynchronizationContext synchronizationContext)
        {
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException();
        }

        public void ExecuteSynchronized(Action action)
        {
            _synchronizationContext.Send(_ =>
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                    throw;
                }
            }, null);
        }
    }

    public interface IEventBus
    {
        void ExecuteSynchronized(Action action);
    }
}
