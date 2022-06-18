using System;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace BSU.Core.Tests.Util
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

        private sealed class XUnitTarget : TargetWithLayout
        {
            private readonly ITestOutputHelper _outputHelper;
            private DateTime? _startTime;

            public XUnitTarget(ITestOutputHelper outputHelper)
            {
                _outputHelper = outputHelper;
                Layout = NLog.Layouts.Layout.FromString("${level:uppercase=true}|${threadname}|${logger:shortName=true}|${message}");
            }

            protected override void Write(LogEventInfo logEvent)
            {
                try
                {
                    _startTime ??= logEvent.TimeStamp;
                    var delta = logEvent.TimeStamp - (DateTime)_startTime;
                    var message = Layout.Render(logEvent);
                    _outputHelper.WriteLine(FormattableString.Invariant($"{delta.TotalSeconds:F3}|{message}"));
                }
                catch (Exception e)
                {
                    _outputHelper.WriteLine("Error in XUnitTarget logger!");
                }
            }
        }
    }
}
