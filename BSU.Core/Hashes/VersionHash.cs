using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using BSU.Core.Model;
using BSU.CoreCommon;
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

        internal VersionHash(IStorageMod mod)
        {
            Logger.Debug("Building version hash from storage mod {0}", mod.GetUid());
            var hashes = new Dictionary<string, FileHash>();
            foreach (var file in mod.GetFileList())
            {
                hashes.Add(file, new SHA1AndPboHash(mod.OpenFile(file, FileAccess.Read), Utils.GetExtension(file)));
            }

            _hash = BuildHash(hashes);
        }

        // TODO: this is for testing only. VersionHash should be abstracted.
        internal VersionHash(byte[] hash)
        {
            _hash = hash;
        }

        internal VersionHash(IRepositoryMod mod)
        {
            Logger.Debug("Building version hash from storage mod {0}", mod.GetUid());
            _hash = BuildHash(mod.GetFileList().ToDictionary(h => h, mod.GetFileHash));
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

        public static VersionHash CreateEmpty()
        {
            using var sha1 = SHA1.Create();
            return new VersionHash(sha1.ComputeHash(new byte[0]));
        }

        public override string ToString() => GetHashString();
    }
}
