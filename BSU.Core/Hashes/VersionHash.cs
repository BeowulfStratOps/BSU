using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;
using BSU.Hashes;

namespace BSU.Core.Hashes
{
    /// <summary>
    /// Hash to determine whether two file-sets match exactly.
    /// </summary>
    public class VersionHash
    {
        private readonly byte[] _hash;

        // TODO: use more specialized interface to get files
        public static async Task<VersionHash> CreateAsync(IStorageMod mod, CancellationToken cancellationToken)
        {
            var hashes = new Dictionary<string, FileHash>();
            foreach (var file in await mod.GetFileList(cancellationToken))
            {
                var stream = await mod.OpenRead(file, cancellationToken);
                var hash = await SHA1AndPboHash.BuildAsync(stream, Utils.GetExtension(file), cancellationToken);
                hashes.Add(file, hash);
            }

            return new VersionHash(BuildHash(hashes));
        }

        private VersionHash(byte[] hash)
        {
            _hash = hash;
        }

        // TODO: use more specialized interface to get files
        public static async Task<VersionHash> CreateAsync(IRepositoryMod mod, CancellationToken cancellationToken)
        {
            var files = await mod.GetFileList(cancellationToken);
            var hashes = new Dictionary<string, FileHash>();
            foreach (var file in files)
            {
                var hash = await mod.GetFileHash(file, cancellationToken);
                hashes.Add(file, hash);
            }
            return new VersionHash(BuildHash(hashes));
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
            return new VersionHash(sha1.ComputeHash(Array.Empty<byte>()));
        }

        public override string ToString() => GetHashString();

        public static VersionHash FromDigest(string hashString)
        {
            return new VersionHash(Utils.FromHexString(hashString));
        }
    }
}
