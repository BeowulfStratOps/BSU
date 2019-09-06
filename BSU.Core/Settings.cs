﻿using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace BSU.Core
{
    class Settings
    {
        private readonly FileInfo _path;
        private readonly SettingsData _data;

        private Settings(FileInfo path, SettingsData data)
        {
            _path = path;
            _data = data;
        }

        public static Settings Load(FileInfo path)
        {
            if (!path.Exists) return new Settings(path, new SettingsData());
            var json = File.ReadAllText(path.FullName);
            var data = JsonConvert.DeserializeObject<SettingsData>(json);
            return new Settings(path, data);
        }

        public void Store()
        {
            var json = JsonConvert.SerializeObject(_data);
            File.WriteAllText(_path.FullName, json);
        }

        public List<RepoEntry> Repositories => _data.Repositories;
        public List<StorageEntry> Storages => _data.Storages;

        private class SettingsData
        {
            public SettingsData()
            {
                Repositories = new List<RepoEntry>();
                Storages = new List<StorageEntry>();
            }

            public readonly List<RepoEntry> Repositories;
            public readonly List<StorageEntry> Storages;
        }
    }

    internal class StorageEntry
    {
        public string Name;
        public string Type;
        public string Path;
        public Dictionary<string, string> Updating;
    }

    internal class RepoEntry
    {
        public string Name;
        public string Type;
        public string Url;
    }
}
