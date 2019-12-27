using System.Collections.Generic;
using System.Linq;

namespace BSU.Hashes
{
    public static class Utils
    {
        public static string GetExtension(string path)
        {
            return path.Split('.').Last();
        }

        public static string ToHexString(IEnumerable<byte> data) => string.Join("", data.Select(b => $"{b:x2}"));
    }
}
