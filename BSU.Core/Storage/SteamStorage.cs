using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;
using Microsoft.Win32;
using NLog;

namespace BSU.Core.Storage
{
    /// <summary>
    /// Steam workshop folder. Read only.
    /// </summary>
    public class SteamStorage : IStorage
    {
        private readonly DirectoryInfo _basePath;
        private Dictionary<string, IStorageMod> _mods;
        private readonly Task _loading;

        public SteamStorage(string path)
        {
            _basePath = new DirectoryInfo(path);
            _loading = Load(CancellationToken.None);
        }

        private async Task Load(CancellationToken cancellationToken)
        {
            // TODO: async?
            var folders = new List<DirectoryInfo>();
            if (!_basePath.Exists) throw new FileNotFoundException(); // TODO: useful error

            foreach (var mod in _basePath.EnumerateDirectories())
            {
                var addonDir = new DirectoryInfo(Path.Combine(mod.FullName, "addons"));
                if (!addonDir.Exists) continue;
                folders.Add(mod);
            }

            _mods = folders.ToDictionary(di => di.Name, di => (IStorageMod) new SteamMod(di, this));
        }

        public async Task<Dictionary<string, IStorageMod>> GetMods(CancellationToken cancellationToken)
        {
            await _loading;
            return _mods;
        }

        public string GetLocation() => _basePath.FullName;

        public Task<IStorageMod> CreateMod(string identifier, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Storage not writable");
        }

        public Task RemoveMod(string identifier, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Storage not writable");
        }

        public bool CanWrite() => false;

        public static string GetWorkshopPath()
        {
            var path = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Valve\Steam", "InstallPath", null);
            if (path == null)
            {
                LogManager.GetCurrentClassLogger().Error("Couldn't find steam install path");
                return null;
            }
            path = Path.Join(path, "steamapps", "workshop", "content", "107410");
            if (Directory.Exists(path)) return path;
            LogManager.GetCurrentClassLogger().Error("Couldn't find arma workshop path");
            return null;
        }
    }
}
