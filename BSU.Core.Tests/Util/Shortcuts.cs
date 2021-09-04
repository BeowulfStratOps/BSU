using System.Threading;
using BSU.Core.Hashes;
using BSU.Core.Model;

namespace BSU.Core.Tests.Util
{
    internal static class Shortcuts
    {
        public static MatchHash GetMatchHash(this IModelStorageMod storageMod)
        {
            return storageMod.GetMatchHash(CancellationToken.None).Result;
        }

        public static VersionHash GetVersionHash(this IModelStorageMod storageMod)
        {
            return storageMod.GetVersionHash(CancellationToken.None).Result;
        }
    }
}
