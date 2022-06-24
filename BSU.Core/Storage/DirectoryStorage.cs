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
        private readonly ILogger _logger;

        private readonly string _path;

        private Dictionary<string, IStorageMod>? _mods;

        private readonly Task _loading;
        private readonly IJobManager _jobManager;

        public DirectoryStorage(string path, IJobManager jobManager)
        {
            _path = path;
            _logger = LogHelper.GetLoggerWithIdentifier(this, path.Split('/', '\\')[^1]);
            _loading = jobManager.Run($"Load Directory Storage {path}", () => Load(CancellationToken.None), CancellationToken.None);
            _jobManager = jobManager;
        }

        private Task Load(CancellationToken cancellationToken)
        {
            if (!new DirectoryInfo(_path).Exists) throw new DirectoryNotFoundException();
            // TODO: async?
            _mods = new DirectoryInfo(_path).EnumerateDirectories("@*")
                .ToDictionary(di => di.Name, di => (IStorageMod) new DirectoryMod(di, this, _jobManager));
            return Task.CompletedTask;
        }

        public async Task<Dictionary<string, IStorageMod>> GetMods(CancellationToken cancellationToken)
        {
            await _loading;
            return _mods!;
        }

        public async Task<IStorageMod> CreateMod(string identifier, CancellationToken cancellationToken)
        {
            await _loading;
            // TODO: async?
            _logger.Debug($"Creating mod {identifier}");
            var dir = new DirectoryInfo(Path.Combine(_path, identifier));
            if (dir.Exists) throw new InvalidOperationException("Path exists");
            dir.Create();
            return new DirectoryMod(dir, this, _jobManager);
        }

        public async Task RemoveMod(string identifier, CancellationToken cancellationToken)
        {
            await _loading;
            // TODO: async?
            var dir = new DirectoryInfo(Path.Combine(_path, "@" + identifier));
            if (!dir.Exists) throw new InvalidOperationException("Path doesn't exist");
            dir.Delete(true);
        }

        public string Location() => _path;

        public bool CanWrite() => true;
    }
}
