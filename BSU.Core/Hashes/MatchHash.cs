using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;

namespace BSU.Core.Hashes
{
    /// <summary>
    /// Hash to determine whether two mod file-sets belong to the same mod. e.g. ace 3.5 and ace 3.12 are both ACE3
    /// </summary>
    public class MatchHash
    {
        private const float Threshold = 0.8f; // at least 80% pbo names match required

        private static readonly Regex AddonsPboRegex =
            new Regex("^/addons/.*\\.pbo$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly string? _name;
        private readonly HashSet<string> _pboNames;

        // TODO: use more specialized interface to get files
        public static async Task<MatchHash> CreateAsync(IStorageMod mod, CancellationToken cancellationToken)
        {
            var modCpp = await mod.OpenRead("/mod.cpp", cancellationToken);

            string? name = null;

            if (modCpp != null)
            {
                using var reader = new StreamReader(modCpp);
                var entries = Util.ParseModCpp(reader.ReadToEnd());
                name = entries.GetValueOrDefault("name");
                if (name != null)
                {
                    name = CleanName(name);
                }
            }

            var pboNames = await mod.GetFileList(cancellationToken);
            var pboNameSet = pboNames.Where(p => AddonsPboRegex.IsMatch(p)).ToHashSet();

            return new MatchHash(pboNameSet, name);
        }

        private static string CleanName(string name)
        {
            return string.Join("",
                    name.Where(c =>
                        "abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(c)))
                .ToLowerInvariant(); // TODO: get some holy water
        }

        // TODO: use more specialized interface to get files
        public static async Task<MatchHash> CreateAsync(IRepositoryMod mod, CancellationToken cancellationToken)
        {
            byte[]? modCppData = null;
            try
            {
                modCppData = await mod.GetFile("/mod.cpp", cancellationToken);
            }
            catch (Exception e)
            {
                // ignored
            }

            string? name = null;
            if (modCppData != null)
            {
                name = Util.ParseModCpp(Encoding.UTF8.GetString(modCppData)).GetValueOrDefault("name");
                if (name != null) name = CleanName(name);
            }

            var pboNames = await mod.GetFileList(cancellationToken);
            var pboNameSet = pboNames.Where(f => AddonsPboRegex.IsMatch(f)).ToHashSet();

            return new MatchHash(pboNameSet, name);
        }

        /// <summary>
        /// Calculates whether two mods MatchHashes belong to the same mod.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>

        public bool IsMatch(MatchHash other)
        {
            // TODO: improve this by A LOT
            // TODO: include folder name
            // TODO: include key names
            // TODO: do fuzzy name matching
            // TODO: do some ~machine learning~ statistics on existing mods
            // TODO: figure out how to handle false positives / false negatives as user
            // TODO: mod.cpp should be near identical for same mod
            // TODO: check config for common names?
            if (_name != null && other._name != null) return _name == other._name;

            var all = new HashSet<string>(_pboNames);
            foreach (var pbo in other._pboNames) all.Add(pbo);

            if (_pboNames.Count / (float) all.Count < Threshold) return false;
            if (other._pboNames.Count / (float) all.Count < Threshold) return false;
            return true;
        }

        // TODO: internal is for testing only. MatchHash should be abstracted.
        internal MatchHash(IEnumerable<string> pboNames, string? name)
        {
            _pboNames = new HashSet<string>(pboNames);
            _name = name;
        }

        public static MatchHash CreateEmpty()
        {
            return new MatchHash(Array.Empty<string>(), null);
        }
    }
}
