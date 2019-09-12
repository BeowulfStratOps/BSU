using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BSU.Util
{
    public static class Util
    {
        public static string GetDisplayName(string modCpp, List<string> keynames)
        {
            // TODO: do something way smarter here

            if (modCpp == null) return "Unknown";
            var modData = ParseModCpp(modCpp);

            var name = modData["name"];

            if (modData.TryGetValue("version", out var version)) return name + " - " + version;

            if (keynames == null || keynames.Count == 0) return name + " - Unknown version";

            version = keynames[0];
            version = string.Join("", version.Where(c => "01234556789._".Contains(c)));
            version = version.Trim('.', '_');
            return name + " - " + version;
        }

        public static Dictionary<string, string> ParseModCpp(string data)
        {
            data = data.Replace("\r", "");
            var matches = Regex.Matches(data, "^([a-zA-Z_]+)\\s*=\\s*\"([^\"]*)\";$", RegexOptions.Multiline);
            return matches.ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
        }
    }
}
