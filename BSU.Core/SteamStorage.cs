﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.CoreInterface;

namespace BSU.Core
{
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

        public List<ILocalMod> GetMods() => _mods.Select(di => (ILocalMod)new SteamMod(di)).ToList();

        public string GetLocation() => _basePath.FullName;

        public string GetName() => _name;

        public bool CanWrite() => false;
    }

    public class SteamMod : DirectoryMod
    {
        public SteamMod(DirectoryInfo directory) : base(directory)
        {
        }
    }
}