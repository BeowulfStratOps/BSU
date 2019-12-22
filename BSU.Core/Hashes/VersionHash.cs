﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BSU.CoreCommon;
using System.Security.Cryptography;
using BSU.Hashes;
using NLog;

namespace BSU.Core.Hashes
{
    public class VersionHash
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly byte[] _hash;

        public VersionHash(IStorageMod mod)
        {
            Logger.Debug("Building version hash from storage mod {0} / {1}", mod.GetStorage().GetIdentifier(), mod.GetIdentifier());
            var hashes = new Dictionary<string, FileHash>();
            foreach (var file in mod.GetFileList())
            {
                hashes.Add(file, new SHA1AndPboHash(mod.GetFile(file), Utils.GetExtension(file)));
            }

            _hash = BuildHash(hashes);
        }

        public VersionHash(IRepositoryMod mod)
        {
            Logger.Debug("Building version hash from storage mod {0}}", mod.GetIdentifier());
            _hash = BuildHash(mod.GetFileList().ToDictionary(h => h, mod.GetFileHash));
        }

        public bool IsMatch(VersionHash other)
        {
            return _hash.SequenceEqual(other._hash);
        }

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
