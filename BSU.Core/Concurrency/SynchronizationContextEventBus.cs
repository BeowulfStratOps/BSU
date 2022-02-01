using System;
using System.Threading;
using NLog;

namespace BSU.Core.Concurrency
{
    public class SynchronizationContextDispatcher : IDispatcher
    {
        private readonly SynchronizationContext _synchronizationContext;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public SynchronizationContextDispatcher(SynchronizationContext synchronizationContext)
        {
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
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

    public interface IDispatcher
    {
        void ExecuteSynchronized(Action action);
    }
}
