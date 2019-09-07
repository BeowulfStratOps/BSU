using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BSU.CoreInterface;

namespace BSU.Core
{
    public class DirectoryStorage : IStorage
    {
        private readonly string _path, _name;

        public DirectoryStorage(string path, string name)
        {
            _path = path;
            _name = name;
        }

        public virtual List<ILocalMod> GetMods()
        {
            return GetModFolders().Select(di => (ILocalMod)new DirectoryMod(di)).ToList();
        }

        protected List<DirectoryInfo> GetModFolders()
        {
            return new DirectoryInfo(_path).EnumerateDirectories("@*").ToList();
        }

        public string GetLocation() => _path;

        public string GetName() => _name;
    }

    public class DirectoryMod : ILocalMod
    {
        private readonly DirectoryInfo _dir;

        public DirectoryMod(DirectoryInfo dir)
        {
            _dir = dir;
        }

        public virtual bool CanWrite() => true;

        public DirectoryInfo GetBaseDirectory() => _dir;

        public string GetDisplayName()
        {
            // TODO: do something way smarter here

            var modcpp = new FileInfo(Path.Combine(_dir.FullName, "mod.cpp"));
            if (!modcpp.Exists) return "Unknown";
            var modData = ParseModCpp(File.ReadAllText(modcpp.FullName));

            var name = modData["name"];

            if (modData.TryGetValue("version", out var version)) return name + " - " + version;

            var keyDir = new DirectoryInfo(Path.Combine(_dir.FullName, "keys"));
            if (!keyDir.Exists) return name + " - Unknown version";
            var keys = keyDir.EnumerateFiles("*.bikey").ToList();
            if (!keys.Any()) return name + " - Unknown version";

            version = keys[0].Name;
            version = version.Substring(0, version.Length - keys[0].Extension.Length);
            version = string.Join("", version.Where(c => "01234556789._".Contains(c)));
            version = version.Trim('.', '_');
            return name + " - " + version;
        }

        public string GetIdentifier() => _dir.Name;


        private static Dictionary<string, string> ParseModCpp(string data)
        {
            data = data.Replace("\r", "");
            var matches = Regex.Matches(data, "^([a-zA-Z_]+)\\s*=\\s*\"([^\"]*)\";$", RegexOptions.Multiline);
            return matches.ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
        }
    }
}
