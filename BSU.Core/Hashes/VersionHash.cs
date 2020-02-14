using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using BSU.Core.Model;
using BSU.Hashes;
using NLog;

namespace BSU.Core.Hashes
{
    /// <summary>
    /// Hash to determine whether two file-sets match exactly.
    /// </summary>
    public class VersionHash
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly byte[] _hash;

        internal VersionHash(StorageMod mod)
        {
            Logger.Debug("Building version hash from storage mod {0}", mod.Uid);
            var hashes = new Dictionary<string, FileHash>();
            foreach (var file in mod.Implementation.GetFileList())
            {
                hashes.Add(file, new SHA1AndPboHash(mod.Implementation.GetFile(file), Utils.GetExtension(file)));
            }

            _hash = BuildHash(hashes);
#if SlowMode
            Thread.Sleep(4*1337);
#endif
        }

        internal VersionHash(RepositoryMod mod)
        {
            Logger.Debug("Building version hash from storage mod {0}", mod.Uid);
            _hash = BuildHash(mod.Implementation.GetFileList().ToDictionary(h => h, mod.Implementation.GetFileHash));
        }

        /// <summary>
        /// Determines whether two VersionHash match exactly.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsMatch(VersionHash other)
        {
            return other != null && _hash.SequenceEqual(other._hash);
        }

        /// <summary>
        /// Builds a single hash sum over all the files and their respective hashes.
        /// </summary>
        /// <param name="hashes"></param>
        /// <returns></returns>
        // TODO: using a dictionary doesn't make any sense. Just cba to rewrite the ordering right now.
        private static byte[] BuildHash(Dictionary<string, FileHash> hashes)
        {
            var builder = new StringBuilder();
            foreach (var (key, value) in hashes.OrderBy(kv => kv.Key))
            {
                builder.Append(key.ToLowerInvariant());
                builder.Append(Utils.ToHexString(value.GetBytes()));
            }

            using var sha1 = SHA1.Create();
            return sha1.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
        }

        public string GetHashString() => Utils.ToHexString(_hash);
    }
}
