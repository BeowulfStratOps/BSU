using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BSU.CoreInterface;

namespace BSU.BSO.Hashes
{
    class MatchHash
    {
        private const float Threshold = 0.8f; // at least 80% pbo names match required

        public string Name;
        public HashSet<string> PboNames;

        public static MatchHash FromLocalMod(ILocalMod mod)
        {
            var dir = mod.GetBaseDirectory();
            var modCpp = new FileInfo(Path.Combine(dir.FullName, "mod.cpp"));
            var hash = new MatchHash();
            if (modCpp.Exists)
            {
                var name = Util.Util.ParseModCpp(File.ReadAllText(modCpp.FullName)).GetValueOrDefault("name");
                if (name != null) hash.Name = CleanName(name);
            }

            var addonsFolder =
                new DirectoryInfo(Path.Combine(dir.FullName,
                    "addons")); // TODO: get some helper functions for this shit..
            if (!addonsFolder.Exists) throw new FileNotFoundException();
            hash.PboNames = addonsFolder.EnumerateFiles("*.pbo").Select(fi => fi.Name.ToLowerInvariant()).ToHashSet();
            return hash;
        }

        private static string CleanName(string name)
        {
            return string.Join("",
                    name.Where(c =>
                        "abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(c)))
                .ToLowerInvariant(); // TODO: kill yourself
        }

        public static MatchHash FromRemoteMod(BsoRepoMod mod)
        {
            var fileList = mod.GetFileList();
            var hash = new MatchHash();
            var modCppHash = fileList.FirstOrDefault(h => h.FileName.ToLowerInvariant() == "/mod.cpp");
            if (modCppHash != null)
            {
                var modCppData = mod.DownloadFile(modCppHash.FileName);
                var name = Util.Util.ParseModCpp(Encoding.UTF8.GetString(modCppData)).GetValueOrDefault("name");
                if (name != null) hash.Name = CleanName(name);
            }

            hash.PboNames = fileList.Select(h => h.FileName.ToLowerInvariant())
                .Where(f => Regex.IsMatch(f, "addons/[^/]*.pbo")).ToHashSet();

            return hash;
        }

        public bool IsMatch(MatchHash other)
        {
            if (Name != null && other.Name != null) return Name == other.Name;

            var all = new HashSet<string>(PboNames);
            foreach (var pbo in other.PboNames) all.Add(pbo);

            if (PboNames.Count / (float) all.Count < Threshold) return false;
            if (other.PboNames.Count / (float)all.Count < Threshold) return false;
            return true;
        }
    }
}