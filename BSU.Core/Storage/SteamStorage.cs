using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.CoreCommon;

namespace BSU.Core.Storage
{
    public class SteamStorage : IStorage
    {
        private string _name;
        private DirectoryInfo _basePath;
        private List<DirectoryInfo> _mods;

        private Uid _uid = new Uid();

        public Uid GetUid() => _uid;

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

        public List<IStorageMod> GetMods() => _mods.Select(di => (IStorageMod) new SteamMod(di, this)).ToList();

        public string GetLocation() => _basePath.FullName;

        public string GetIdentifier() => _name;

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