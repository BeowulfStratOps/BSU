using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Model;

namespace BSU.Core.Tests.Util
{
    internal static class Shortcuts
    {
        public static async Task<MatchHash> GetMatchHash(this IModelStorageMod storageMod)
        {
            return await storageMod.GetMatchHash(CancellationToken.None);
        }

        public static async Task<VersionHash> GetVersionHash(this IModelStorageMod storageMod)
        {
            return await storageMod.GetVersionHash(CancellationToken.None);
        }
    }
}
