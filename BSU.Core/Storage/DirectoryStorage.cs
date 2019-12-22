using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Storage
{
    // TODO: document that this is (aggressively) using normalized (=lower case) paths!
    public class DirectoryStorage : IStorage
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly string _path, _name;

        public DirectoryStorage(string path, string name)
        {
            _path = path;
            _name = name;
            if (!new DirectoryInfo(path).Exists) throw new DirectoryNotFoundException();
        }

        public virtual List<IStorageMod> GetMods()
        {
            return GetModFolders().Select(di => (IStorageMod)new DirectoryMod(di, this)).ToList();
        }

        private List<DirectoryInfo> GetModFolders()
        {
            return new DirectoryInfo(_path).EnumerateDirectories("@*").ToList();
        }

        public string GetLocation() => _path;

        public string GetIdentifier() => _name;
        public IStorageMod CreateMod(string identifier)
        {
            Logger.Debug("Creating mod {0}", identifier);
            var dir = new DirectoryInfo(Path.Combine(_path, "@" + identifier));
            if (dir.Exists) throw new InvalidOperationException("Path exists");
            dir.Create();
            return new DirectoryMod(dir, this);
        }

        public virtual bool CanWrite() => true;
    }
}
