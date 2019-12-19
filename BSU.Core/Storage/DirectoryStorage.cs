using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.CoreCommon;

namespace BSU.Core.Storage
{
    // TODO: document that this is (aggressively) using normalized (=lower case) paths!
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

        private List<DirectoryInfo> GetModFolders()
        {
            return new DirectoryInfo(_path).EnumerateDirectories("@*").ToList();
        }

        public string GetLocation() => _path;

        public string GetIdentifier() => _name;
        public ILocalMod CreateMod(string identifier)
        {
            var dir = new DirectoryInfo(Path.Combine(_path, "@" + identifier));
            if (dir.Exists) throw new InvalidOperationException("Path exists");
            dir.Create();
            return new DirectoryMod(dir, this);
        }

        public virtual bool CanWrite() => true;
    }
}
