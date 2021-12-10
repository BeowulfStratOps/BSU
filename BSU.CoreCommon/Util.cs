using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BSU.CoreCommon
{
    /// <summary>
    /// Utility methods used by repo/storage types and core
    /// </summary>
    public static class Util
    {
        /// <summary>
        /// Attempt to create a display name
        /// </summary>
        /// <param name="modCpp">/mod.cpp content</param>
        /// <param name="keynames">Names of .bikey files</param>
        /// <returns></returns>
        public static (string name, string version) GetDisplayInfo(string? modCpp, List<string>? keynames)
        {
            // TODO: do something way smarter here

            if (modCpp == null) return ("Unknown", "Unknown");
            var modData = ParseModCpp(modCpp);

            var name = modData.GetValueOrDefault("name");
            if (name == null) return ("Unknown", "Unknown");

            if (modData.TryGetValue("version", out var version)) return (name, version);

            if (keynames == null || keynames.Count == 0) return (name, "Unknown");

            version = keynames[0];
            version = string.Join("", version.Where(c => "01234556789._".Contains(c)));
            version = version.Trim('.', '_');
            return (name,  version);
        }

        /// <summary>
        /// Parse simple mod.cpp contents
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Dictionary<string, string> ParseModCpp(string data)
        {
            data = data.Replace("\r", "");
            var matches = Regex.Matches(data, "^([a-zA-Z_]+)\\s*=\\s*\"([^\"]*)\";$", RegexOptions.Multiline);
            return matches.ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
        }

        /// <summary>
        /// Check whether a path complies to relative path format.
        /// I.e. lower case, forward slashes, starting with forward slash, not ending with forward slash.
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="FormatException">Throws an exception if path doesn't comply.</exception>
        public static void CheckPath(string path)
        {
            if (!path.StartsWith('/')) throw new FormatException($"Path '{path}' should start with a '/'");
            if (path.EndsWith('/')) throw new FormatException(path);
            if (path.Contains('\\')) throw new FormatException(path);
            if (path.ToLowerInvariant() != path) throw new FormatException(path);
        }
    }
}
