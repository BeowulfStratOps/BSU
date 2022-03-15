using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Launch;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Storage
{
    /// <summary>
    /// Steam workshop folder. Read only.
    /// </summary>
    public class SteamStorage : IStorage
    {
        private readonly DirectoryInfo? _basePath;
        private Dictionary<string, IStorageMod>? _mods;
        private readonly Task _loading;

        public SteamStorage()
        {
            var workshopPath = GetWorkshopPath();
            if (workshopPath == null)
            {
                _basePath = null!;
                _loading = Task.FromException(new Exception("Failed to find workshop path"));
                return;
            }
            _basePath = new DirectoryInfo(workshopPath);
            _loading = Task.Run(() => Load(CancellationToken.None));
        }

        private Task Load(CancellationToken cancellationToken)
        {
            // TODO: async?
            var folders = new List<DirectoryInfo>();
            if (!_basePath!.Exists) throw new FileNotFoundException(); // TODO: useful error

            foreach (var mod in _basePath.EnumerateDirectories())
            {
                var addonDir = new DirectoryInfo(Path.Combine(mod.FullName, "addons"));
                if (!addonDir.Exists) continue;
                folders.Add(mod);
            }

            _mods = folders.ToDictionary(di => di.Name, di => (IStorageMod) new SteamMod(di, this));
            return Task.CompletedTask;
        }

        public async Task<Dictionary<string, IStorageMod>> GetMods(CancellationToken cancellationToken)
        {
            await _loading;
            return _mods!;
        }

        public Task<IStorageMod> CreateMod(string identifier, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Storage not writable");
        }

        public Task RemoveMod(string identifier, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Storage not writable");
        }

        public string Location() => _basePath?.FullName ?? "???";

        public bool CanWrite() => false;

        private static string? GetWorkshopPath()
        {
            var armaPath = ArmaData.GetGamePath();
            if (armaPath == null) return null;

            var path = Path.Join(armaPath, "..", "..", "workshop", "content", "107410");
            path = Path.GetFullPath(path);
            if (Directory.Exists(path)) return path;
            LogManager.GetCurrentClassLogger().Error($"Couldn't find arma workshop path. Tried {path}");
            return null;
        }
    }
}
