using System;
using System.Text.RegularExpressions;
using NLog;

namespace BSU.Core
{
    public static class LogHelper
    {
        public static ILogger GetLoggerWithIdentifier(object entity, string rawIdentifier)
        {
            return GetLoggerWithIdentifier(entity.GetType(), rawIdentifier);
        }

        public static Logger GetLoggerWithIdentifier(Type entityType, string rawIdentifier)
        {
            var identifierCleaned = Regex.Replace(rawIdentifier, "\\W", "_");
            return LogManager.GetLogger($"{entityType.Name}-{identifierCleaned}");
        }
    }
}
