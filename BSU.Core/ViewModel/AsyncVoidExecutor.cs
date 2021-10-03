using System;
using System.Threading.Tasks;
using NLog;

namespace BSU.Core.ViewModel
{
    public interface IAsyncVoidExecutor
    {
        void Execute(Func<Task> action);
    }

    public class AsyncVoidExecutor : IAsyncVoidExecutor
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public async void Execute(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception e)
            {
                _logger.Error(e);
                throw;
            }
        }
    }
}
