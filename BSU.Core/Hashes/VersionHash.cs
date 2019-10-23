using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BSU.CoreInterface;
using System.Security.Cryptography;

namespace BSU.Core.Hashes
{
    public class VersionHash
    {
        private Dictionary<string, byte[]> Hashes;
        private readonly byte[] Hash;

        public VersionHash(ILocalMod mod)
        {
            Hashes = new Dictionary<string, byte[]>();
            foreach (var file in mod.GetFileList())
            {
                var hash = GetFileHash(file, mod.GetFile(file));
                Hashes.Add(file, hash);
            }

            Hash = BuildHash();
        }

        private static byte[] GetFileHash(string path, Stream fileStream)
        {
            if ((path.EndsWith(".pbo") || path.EndsWith(".ebo")) && fileStream.Length > 20 && fileStream.CanSeek)
            {
                var array = new byte[20];
                fileStream.Seek(-20L, SeekOrigin.End);
                fileStream.Read(array, 0, 20);
                return array;
            }

            // TODO: use MurmurHash.
            using var sha1 = SHA1.Create();
            return sha1.ComputeHash(fileStream);
        }

        public VersionHash(IRemoteMod mod)
        {
            Hashes = mod.GetFileList().ToDictionary(h => h.GetPath(), h => h.GetFileHash());
            Hash = BuildHash();
        }

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
                builder.Append(ToHexString(kv.Value));
            }
            using var sha1 = SHA1.Create();
            return sha1.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
        }

        private static string ToHexString(byte[] data) => string.Join("", data.Select(b => $"{b:x2}"));

        public string GetHashString() => ToHexString(Hash);
    }
}
