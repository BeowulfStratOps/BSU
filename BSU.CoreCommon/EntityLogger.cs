using System;
using System.Diagnostics;
using System.Threading;
using NLog;

namespace BSU.CoreCommon
{
    public static class EntityLogger
    {
        private static int _nextId;

        public static Logger GetLogger()
        {
            var callingClass = GetTypeOfCallingClass();
            var id = Interlocked.Increment(ref _nextId);
            var logger = LogManager.GetLogger($"{callingClass.FullName}-{id}");
            logger.SetProperty("uid", id);
            return logger;
        }

        private static Type GetTypeOfCallingClass()
        {
            Type declaringType;
            var skipFrames = 2;
            do
            {
                var method = new StackFrame(skipFrames, false).GetMethod();
                declaringType = method?.DeclaringType;
                if (declaringType == null)
                {
                    return null;
                }
                skipFrames++;
            }
            while (declaringType.Module.Name.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase));

            return declaringType;
        }

        public static int GetId(this Logger logger)
        {
            return logger.Properties.TryGetValue("uid", out var result) ? (int)result : throw new InvalidOperationException("Logger is not linked to an entity.");
        }
    }
}
