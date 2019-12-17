﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BSU.CoreInterface;

namespace BSU.Core.Hashes
{
    internal class MatchHash
    {
        private const float Threshold = 0.8f; // at least 80% pbo names match required

        private static Regex _addonsPboRegex = new Regex("^/addons/.*\\.pbo", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string Name;
        public HashSet<string> PboNames;

        public MatchHash(ILocalMod mod)
        {
            var modCpp = mod.GetFile("/mod.cpp");
            if (modCpp != null)
            {
                using var reader = new StreamReader(modCpp);
                var entries = Util.Util.ParseModCpp(reader.ReadToEnd());
                var name = entries.GetValueOrDefault("name");
                if (name != null) Name = CleanName(name);
            }
            PboNames = mod.GetFileList().Where(p => _addonsPboRegex.IsMatch(p)).ToHashSet();
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
                if (name != null) Name = CleanName(name);
            }

            PboNames = mod.GetFileList().Where(f => _addonsPboRegex.IsMatch(f)).ToHashSet();
        }

        public bool IsMatch(MatchHash other)
        {
            // TODO: improve this by A LOT. fuzzy name matching. maybe do some learning on existent mods/updates?
            // TODO: figure out how to handle false positives / false negatives as user
            if (Name != null && other.Name != null) return Name == other.Name;

            var all = new HashSet<string>(PboNames);
            foreach (var pbo in other.PboNames) all.Add(pbo);

            if (PboNames.Count / (float) all.Count < Threshold) return false;
            if (other.PboNames.Count / (float)all.Count < Threshold) return false;
            return true;
        }
    }
}