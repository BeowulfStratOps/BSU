using NLog;
using NLog.Config;
using Xunit.Abstractions;

namespace BSU.Core.Tests
{
    public class LoggedTest
    {
        protected readonly ITestOutputHelper OutputHelper;

        protected LoggedTest(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;
            var config = new LoggingConfiguration();
            var target = new XUnitTarget(outputHelper);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, target);
            LogManager.Configuration = config;
        }
    }
}