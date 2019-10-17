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
            var dir = mod.GetBaseDirectory();
            foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var relativePath = file.FullName.Replace(dir.FullName, "").Replace("\\", "/");
                var hash = GetFileHash(file);
                Hashes.Add(relativePath, hash);
            }

            Hash = BuildHash();
        }

        private static byte[] GetFileHash(FileInfo file)
        {
            using var fileStream = file.OpenRead();
            if ((file.Extension == ".pbo" || file.Extension == ".ebo") && file.Length > 20)
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
