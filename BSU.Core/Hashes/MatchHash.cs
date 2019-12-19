using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BSU.CoreCommon;

namespace BSU.Core.Hashes
{
    internal class MatchHash
    {
        private const float Threshold = 0.8f; // at least 80% pbo names match required

        private static readonly Regex AddonsPboRegex =
            new Regex("^/addons/.*\\.pbo", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly string _name;
        private readonly HashSet<string> _pboNames;

        public MatchHash(ILocalMod mod)
        {
            var modCpp = mod.GetFile("/mod.cpp");
            if (modCpp != null)
            {
                using var reader = new StreamReader(modCpp);
                var entries = Util.Util.ParseModCpp(reader.ReadToEnd());
                var name = entries.GetValueOrDefault("name");
                if (name != null) _name = CleanName(name);
            }
            _pboNames = mod.GetFileList().Where(p => AddonsPboRegex.IsMatch(p)).ToHashSet();
        }

        private static string CleanName(string name)
        {
            return string.Join("",
                    name.Where(c =>
                        "abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(c)))
                .ToLowerInvariant(); // TODO: get some holy water
        }

        public MatchHash(IRemoteMod mod)
        {
            var modCppData = mod.GetFile("/mod.cpp");
            if (modCppData != null)
            {
                var name = Util.Util.ParseModCpp(Encoding.UTF8.GetString(modCppData)).GetValueOrDefault("name");
                if (name != null) _name = CleanName(name);
            }

            _pboNames = mod.GetFileList().Where(f => AddonsPboRegex.IsMatch(f)).ToHashSet();
        }

        public bool IsMatch(MatchHash other)
        {
            // TODO: improve this by A LOT
            // TODO: include folder name
            // TODO: include key names
            // TODO: do fuzzy name matching
            // TODO: do some ~machine learning~ statistics on existing mods
            // TODO: figure out how to handle false positives / false negatives as user
            if (_name != null && other._name != null) return _name == other._name;

            var all = new HashSet<string>(_pboNames);
            foreach (var pbo in other._pboNames) all.Add(pbo);

            if (_pboNames.Count / (float) all.Count < Threshold) return false;
            if (other._pboNames.Count / (float)all.Count < Threshold) return false;
            return true;
        }
    }
}
