using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using BSU.Hashes;

namespace BSU.CoreCommon.Hashes
{
    /// <summary>
    /// Hash to determine whether two file-sets match exactly.
    /// </summary>
    [HashClass(HashType.Version, 10)]
    public class VersionHash : IModHash
    {
        // public for serialization
        public byte[] Hash { get; }

        // TODO: use more specialized interface to get files
        public static async Task<VersionHash> CreateAsync(IStorageMod mod, CancellationToken cancellationToken)
        {
            var hashes = new Dictionary<string, FileHash>();
            foreach (var file in await mod.GetFileList(cancellationToken))
            {
                var stream = await mod.OpenRead(file, cancellationToken);
                if (stream == null) throw new InvalidOperationException();
                var hash = await Sha1AndPboHash.BuildAsync(stream, Utils.GetExtension(file), cancellationToken);
                hashes.Add(file, hash);
            }

            return new VersionHash(BuildHash(hashes));
        }

        [JsonConstructor]
        public VersionHash(byte[] hash)
        {
            Hash = hash;
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
        private bool IsMatch(VersionHash other)
        {
            return Hash.SequenceEqual(other.Hash);
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

        public string GetHashString() => Utils.ToHexString(Hash);

        public override string ToString() => GetHashString();
        public bool IsMatch(IModHash other)
        {
            if (other is not VersionHash otherHash) throw new InvalidOperationException();
            return IsMatch(otherHash);
        }
    }
}
