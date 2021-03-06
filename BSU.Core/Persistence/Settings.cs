﻿using System.Collections.Generic;
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
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
            Logger.Debug("Loading settings from {0}", path.FullName);
            if (!path.Exists) return new Settings(path, new SettingsData());
            var json = File.ReadAllText(path.FullName);
            var data = JsonConvert.DeserializeObject<SettingsData>(json);
            return new Settings(path, data);
        }

        public void Store()
        {
            lock (_path)
            {
                Logger.Debug("Saving settings");
                var json = JsonConvert.SerializeObject(_data);
                File.WriteAllText(_path.FullName, json);
            }
        }

        public List<RepositoryEntry> Repositories => _data.Repositories;
        public List<StorageEntry> Storages => _data.Storages;

        private class SettingsData
        {
            public SettingsData()
            {
                Repositories = new List<RepositoryEntry>();
                Storages = new List<StorageEntry>();
            }

            public readonly List<RepositoryEntry> Repositories;
            public readonly List<StorageEntry> Storages;
        }
    }
}
