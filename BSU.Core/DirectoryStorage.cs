using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BSU.Util;
using BSU.CoreInterface;
using BSU.Hashes;

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
            return GetModFolders().Select(di => (ILocalMod)new DirectoryMod(di, this)).ToList();
        }

        protected List<DirectoryInfo> GetModFolders()
        {
            return new DirectoryInfo(_path).EnumerateDirectories("@*").ToList();
        }

        public string GetLocation() => _path;

        public string GetIdentifier() => _name;

        public virtual bool CanWrite() => true;
    }

    public class DirectoryMod : ILocalMod
    {
        private readonly DirectoryInfo _dir;
        private readonly IStorage _parentStorage;

        public DirectoryMod(DirectoryInfo dir, IStorage parentStorage)
        {
            _dir = dir;
            _parentStorage = parentStorage;
        }

        public bool FileExists(string path) => File.Exists(Path.Combine(_dir.FullName, path));

        public DirectoryInfo GetBaseDirectory() => _dir;

        public string GetDisplayName()
        {
            var modcpp = new FileInfo(Path.Combine(_dir.FullName, "mod.cpp"));
            var modData = modcpp.Exists ? File.ReadAllText(modcpp.FullName) : null;

            var keyDir = new DirectoryInfo(Path.Combine(_dir.FullName, "keys"));
            var keys = keyDir.Exists ? keyDir.EnumerateFiles("*.bikey").Select(n => n.Name.Replace(".bikey", "")).ToList() : null;

            return Util.Util.GetDisplayName(modData, keys);
        }

        public Stream GetFile(string path)
        {
            if (path.StartsWith('/') || path.StartsWith('\\')) path = path.Substring(1);
            return File.OpenRead(Path.Combine(_dir.FullName, path));
        }

        public List<string> GetFileList() => _dir.EnumerateFiles("*", SearchOption.AllDirectories)
            .Select(fi => fi.FullName.Replace(_dir.FullName, "").Replace('\\', '/')).ToList();

        public FileHash GetFileHash(string path)
        {
            return new SHA1AndPboHash(GetFile(path), Utils.GetExtension(path));
        }

        public string GetIdentifier() => _dir.Name;

        public IStorage GetStorage() => _parentStorage;
    }
}
