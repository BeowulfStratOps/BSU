using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BSU.CoreInterface;
using System.Security.Cryptography;

namespace BSU.BSO.Hashes
{
    class VersionHash
    {
        public Dictionary<string, byte[]> Hashes;

        public static VersionHash FromLocalMod(ILocalMod mod)
        {
            var hashes = new Dictionary<string, byte[]>();
            var dir = mod.GetBaseDirectory();
            foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var relativePath = file.FullName.Replace(dir.FullName, "").Replace("\\", "/");
                var hash = GetFileHash(file);
                hashes.Add(relativePath, hash);
            }

            return new VersionHash
            {
                Hashes = hashes
            };
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

        public static VersionHash FromRemoteMod(BsoRepoMod mod) => new VersionHash
        {
            Hashes = mod.GetFileList().ToDictionary(h => h.FileName, h => h.Hash)
        };

        public bool Matches(VersionHash other)
        {
            if (Hashes.Count != other.Hashes.Count) return false;
            foreach (var (key, value) in Hashes)
            {
                var otherHash = other.Hashes.GetValueOrDefault(key, null);
                if (otherHash == null) return false;
                if (!value.SequenceEqual(otherHash)) return false;
            }
            return true;
        }
    }
}
