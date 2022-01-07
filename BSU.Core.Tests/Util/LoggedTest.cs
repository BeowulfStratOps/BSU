using System;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit.Abstractions;

namespace BSU.Core.Tests.Util
{
    public class LoggedTest
    {
        protected LoggedTest(ITestOutputHelper outputHelper)
        {
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
                LogManager.Setup().SetupExtensions(s =>
                    s.RegisterLayoutRenderer("time-from-start", logEvent =>
                    {
                        _startTime ??= logEvent.TimeStamp;
                        var delta = logEvent.TimeStamp - (DateTime)_startTime;
                        return FormattableString.Invariant($"{delta.TotalSeconds:F3}");
                    }));
                Layout = NLog.Layouts.Layout.FromString("${time-from-start}|${level:uppercase=true}|${threadname}|${logger:shortName=true}|${message}");
            }

            protected override void Write(LogEventInfo logEvent)
            {
                var message = Layout.Render(logEvent);
                _outputHelper.WriteLine(message);
            }
        }
    }
}
