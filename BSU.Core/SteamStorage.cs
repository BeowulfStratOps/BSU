using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.CoreInterface;

namespace BSU.Core
{
    // TODO: make like 200% sure this never writes!!
    public class SteamStorage : IStorage
    {
        private string _name;
        private DirectoryInfo _basePath;
        private List<DirectoryInfo> _mods;
        public SteamStorage(string path, string name)
        {
            // C:\Program Files (x86)\Steam\steamapps\workshop\content\107410
            _basePath = new DirectoryInfo(Path.Combine(path, "steamapps", "workshop", "content", "107410"));
            _name = name;
            _mods = new List<DirectoryInfo>();
            if (!_basePath.Exists) throw new FileNotFoundException();

            foreach (var mod in _basePath.EnumerateDirectories())
            {
                var addonDir = new DirectoryInfo(Path.Combine(mod.FullName, "addons"));
                if (!addonDir.Exists) continue;
                _mods.Add(mod);
            }
        }

        public List<ILocalMod> GetMods() => _mods.Select(di => (ILocalMod)new SteamMod(di, this)).ToList();

        public string GetLocation() => _basePath.FullName;

        public string GetIdentifier() => _name;
        public ILocalMod CreateMod(string identifier)
        {
            throw new InvalidOperationException();
        }

        public bool CanWrite() => false;
    }

    public class SteamMod : DirectoryMod
    {
        public SteamMod(DirectoryInfo directory, IStorage parentStorage) : base(directory, parentStorage)
        {
        }
    }
}