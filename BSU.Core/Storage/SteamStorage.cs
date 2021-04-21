using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.CoreCommon;

namespace BSU.Core.Storage
{
    /// <summary>
    /// Steam workshop folder. Read only.
    /// </summary>
    public class SteamStorage : IStorage
    {
        private readonly DirectoryInfo _basePath;
        private Dictionary<string, IStorageMod> _mods;

        public SteamStorage(string path)
        {
            // C:\Program Files (x86)\Steam\steamapps\workshop\content\107410
            _basePath = new DirectoryInfo(Path.Combine(path, "steamapps", "workshop", "content", "107410"));
        }

        public void Load()
        {
            var folders = new List<DirectoryInfo>();
            if (!_basePath.Exists) throw new FileNotFoundException(); // TODO: useful error

            foreach (var mod in _basePath.EnumerateDirectories())
            {
                var addonDir = new DirectoryInfo(Path.Combine(mod.FullName, "addons"));
                if (!addonDir.Exists) continue;
                folders.Add(mod);
            }

            _mods = folders.ToDictionary(di => di.Name, di => (IStorageMod) new SteamMod(di, this));
        }

        public Dictionary<string, IStorageMod> GetMods() => _mods;

        public string GetLocation() => _basePath.FullName;

        public IStorageMod CreateMod(string identifier)
        {
            throw new InvalidOperationException("Storage not writable");
        }

        public void RemoveMod(string identifier)
        {
            throw new InvalidOperationException("Storage not writable");
        }

        public bool CanWrite() => false;
    }
}
