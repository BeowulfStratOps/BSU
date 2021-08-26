using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        private readonly Task _loading;

        public DirectoryStorage(string path)
        {
            _path = path;
            if (!new DirectoryInfo(path).Exists) throw new DirectoryNotFoundException();
            _loading = Load(CancellationToken.None);
        }

        private async Task Load(CancellationToken cancellationToken)
        {
            // TODO: async?
            _mods = new DirectoryInfo(_path).EnumerateDirectories("@*")
                .ToDictionary(di => di.Name, di => (IStorageMod) new DirectoryMod(di, this));
        }

        public async Task<Dictionary<string, IStorageMod>> GetMods(CancellationToken cancellationToken)
        {
            await _loading;
            return _mods;
        }

        public string GetLocation() => _path;

        public async Task<IStorageMod> CreateMod(string identifier, CancellationToken cancellationToken)
        {
            await _loading;
            // TODO: async?
            _logger.Debug("Creating mod {0}", identifier);
            var dir = new DirectoryInfo(Path.Combine(_path, identifier));
            if (dir.Exists) throw new InvalidOperationException("Path exists");
            dir.Create();
            return new DirectoryMod(dir, this);
        }

        public async Task RemoveMod(string identifier, CancellationToken cancellationToken)
        {
            await _loading;
            // TODO: async?
            var dir = new DirectoryInfo(Path.Combine(_path, "@" + identifier));
            if (!dir.Exists) throw new InvalidOperationException("Path doesn't exist");
            dir.Delete(true);
        }

        public virtual bool CanWrite() => true;
    }
}
