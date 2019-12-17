using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BSU.CoreInterface;
using System.Security.Cryptography;
using BSU.Hashes;

namespace BSU.Core.Hashes
{
    public class VersionHash
    {
        private Dictionary<string, FileHash> Hashes;
        private readonly byte[] Hash;

        public VersionHash(ILocalMod mod)
        {
            Hashes = new Dictionary<string, FileHash>();
            foreach (var file in mod.GetFileList())
            {
                Hashes.Add(file, new SHA1AndPboHash(mod.GetFile(file), Utils.GetExtension(file)));
            }

            Hash = BuildHash();
        }

        public VersionHash(IRemoteMod mod)
        {
            Hashes = mod.GetFileList().ToDictionary(h => h, mod.GetFileHash);
            Hash = BuildHash();
        }

        // TODO: name: Matches vs IsMatch (as in MatchHash)
        public bool Matches(VersionHash other)
        {
            return Hash.SequenceEqual(other.Hash);
        }

        private byte[] BuildHash()
        {
            var builder = new StringBuilder();
            foreach (var kv in Hashes.OrderBy(kv => kv.Key))
            {
                builder.Append(kv.Key.ToLowerInvariant());
                builder.Append(Utils.ToHexString(kv.Value.GetBytes()));
            }
            using var sha1 = SHA1.Create();
            return sha1.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
        }

        public string GetHashString() => Utils.ToHexString(Hash);
    }
}
