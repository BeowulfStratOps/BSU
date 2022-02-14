using System;
using System.Threading.Tasks;
using NLog;

namespace BSU.Core.ViewModel
{
    public class AsyncVoidExecutor : IAsyncVoidExecutor
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        public async void Execute(Func<Task> action)
        {
            // TODO: capture current stacktrace here, so that we have that info when an exception occurs
            try
            {
                await action();
            }
            catch (Exception e)
            {
                Logger.Error(e);
                throw;
            }
        }
    }

    public interface IAsyncVoidExecutor
    {
        void Execute(Func<Task> action);
    }
}
