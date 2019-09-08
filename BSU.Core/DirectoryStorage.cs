using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BSU.Util;
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
            var modcpp = new FileInfo(Path.Combine(_dir.FullName, "mod.cpp"));
            var modData = modcpp.Exists ? File.ReadAllText(modcpp.FullName) : null;

            var keyDir = new DirectoryInfo(Path.Combine(_dir.FullName, "keys"));
            var keys = keyDir.Exists ? keyDir.EnumerateFiles("*.bikey").Select(n => n.Name.Replace(".bikey", "")).ToList() : null;

            return ModDisplayName.GetDisplayName(modData, keys);
        }

        public string GetIdentifier() => _dir.Name;
    }
}
