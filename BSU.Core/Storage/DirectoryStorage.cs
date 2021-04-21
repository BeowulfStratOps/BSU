using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Storage
{
    /// <summary>
    /// Represents a directory on the local file system, containing mod folders.
    /// </summary>
    // TODO: document that this is (aggressively) using normalized (=lower case) paths!
    public class DirectoryStorage : IStorage
    {
        private readonly Logger _logger = EntityLogger.GetLogger();

        private readonly string _path;

        private Dictionary<string, IStorageMod> _mods;

        public DirectoryStorage(string path)
        {
            _path = path;
            if (!new DirectoryInfo(path).Exists) throw new DirectoryNotFoundException();
        }

        public void Load()
        {
            _mods = new DirectoryInfo(_path).EnumerateDirectories("@*")
                .ToDictionary(di => di.Name, di => (IStorageMod) new DirectoryMod(di, this));
        }

        public Dictionary<string, IStorageMod> GetMods()
        {
            return _mods;
        }

        public string GetLocation() => _path;

        public IStorageMod CreateMod(string identifier)
        {
            _logger.Debug("Creating mod {0}", identifier);
            var dir = new DirectoryInfo(Path.Combine(_path, identifier));
            if (dir.Exists) throw new InvalidOperationException("Path exists");
            dir.Create();
            return new DirectoryMod(dir, this);
        }

        public void RemoveMod(string identifier)
        {
            var dir = new DirectoryInfo(Path.Combine(_path, "@" + identifier));
            if (!dir.Exists) throw new InvalidOperationException("Path doesn't exist");
            dir.Delete(true);
        }

        public virtual bool CanWrite() => true;
    }
}
