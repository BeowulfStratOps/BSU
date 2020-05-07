using NLog;
using NLog.Targets;
using Xunit.Abstractions;

namespace BSU.Core.Tests
{
    public class XUnitTarget : TargetWithLayout
    {
        private readonly ITestOutputHelper _outputHelper;

        public XUnitTarget(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var message = Layout.Render(logEvent);
            _outputHelper.WriteLine(message);
        }
    }
}