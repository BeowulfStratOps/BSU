using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NLog;

namespace BSU.Core.Persistence
{
    /// <summary>
    /// Tracks settings / internal core state in a json file.
    /// </summary>
    internal class Settings : ISettings
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly FileInfo _path;
        private readonly SettingsData _data;

        private Settings(FileInfo path, SettingsData data)
        {
            _path = path;
            _data = data;
        }

        public static Settings Load(FileInfo path)
        {
            path.Refresh();
            LogManager.GetCurrentClassLogger().Debug($"Loading settings from {path.FullName}");
            if (!path.Exists) return new Settings(path, new SettingsData());
            var json = File.ReadAllText(path.FullName);
            var data = JsonConvert.DeserializeObject<SettingsData>(json);
            return new Settings(path, data);
        }

        public void Store()
        {
            lock (_path)
            {
                _logger.Trace("Saving settings");
                var json = JsonConvert.SerializeObject(_data);
                File.WriteAllText(_path.FullName, json);
            }
        }

        public List<RepositoryEntry> Repositories => _data.Repositories;
        public List<StorageEntry> Storages => _data.Storages;

        public bool FirstStartDone
        {
            get => _data.FirstStartDone;
            set => _data.FirstStartDone = value;
        }

        private class SettingsData
        {
            public SettingsData()
            {
                Repositories = new List<RepositoryEntry>();
                Storages = new List<StorageEntry>();
            }

            public readonly List<RepositoryEntry> Repositories;
            public readonly List<StorageEntry> Storages;
            public bool FirstStartDone { get; set; }
        }
    }
}
