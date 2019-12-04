﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BSU.Hashes
{
    public class SHA1AndPboHash : FileHash
    {
        private readonly byte[] _hash;

        public SHA1AndPboHash(Stream file, string extension)
        {
            if ((extension == "pbo" || extension == "ebo") && file.Length > 20 && file.CanSeek)
            {
                _hash = new byte[20];
                file.Seek(-20L, SeekOrigin.End);
                file.Read(_hash, 0, 20);
                return;
            }

            using var sha1 = SHA1.Create();
            _hash = sha1.ComputeHash(file);
        }

        public SHA1AndPboHash(byte[] hash)
        {
            _hash = hash;
        }

        public override byte[] GetBytes() => _hash;
    }
}
