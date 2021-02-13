using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BSU.Hashes
{
    public static class Utils
    {
        public static string GetExtension(string path)
        {
            // TODO: get a library for this /s
            return path.Split('.').Last();
        }

        public static string ToHexString(IEnumerable<byte> data) => string.Join("", data.Select(b => $"{b:x2}"));

        public static byte[] FromHexString(string hashString)
        {
            if (hashString.Length % 2 != 0) throw new ArgumentException();
            var result = new byte[hashString.Length / 2];
            for (int i = 0; i < hashString.Length / 2; i++)
            {
                result[i] = (byte)int.Parse(hashString.Substring(2 * i, 2), NumberStyles.HexNumber);
            }
            return result;
        }
    }
}
