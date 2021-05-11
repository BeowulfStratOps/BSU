using NLog;
using NLog.Targets;
using Xunit.Abstractions;

namespace BSU.Core.Tests
{
    public sealed class XUnitTarget : TargetWithLayout
    {
        private readonly ITestOutputHelper _outputHelper;

        public XUnitTarget(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            Layout = NLog.Layouts.Layout.FromString("${time}|${level:uppercase=true}|${threadname}|${logger:shortName=true}|${message}");
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var message = Layout.Render(logEvent);
            _outputHelper.WriteLine(message);
        }
    }
}
