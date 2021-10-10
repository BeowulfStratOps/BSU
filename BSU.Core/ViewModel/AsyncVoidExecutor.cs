using System;
using System.Threading.Tasks;
using NLog;

namespace BSU.Core.ViewModel
{
    public static class AsyncVoidExecutor
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        public static async void Execute(Func<Task> action)
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
}
