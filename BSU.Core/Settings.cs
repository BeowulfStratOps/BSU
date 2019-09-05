using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BSU.Core
{
    class Settings
    {
        private readonly FileInfo _path;

        public Settings(FileInfo path)
        {
            _path = path;
            Load();
        }

        public static Settings Load()
        {
            throw new NotImplementedException();
        }

        public void Store()
        {
            throw new NotImplementedException();
        }

        public List<Uri> Repositories;
        public List<StorageEntry> Storages; // 
    }

    class StorageEntry
    {
        public string Type;
        public string Path;
        public Dictionary<string, string> Updating;
    }
}
