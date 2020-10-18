using System;
using System.Collections.Generic;

namespace BSU.Core.Persistence
{
    internal interface IStorageEntry
    {
        Guid Guid { get; }
        string Name { get; }
        string Type { get; }
        string Path { get; }
    }

    internal class StorageEntry : IStorageEntry
    {
        public Guid Guid { get; }
        public string Name { get; }
        public string Type { get; }
        public string Path { get; }
        public Dictionary<string, UpdateTarget> Updating = new Dictionary<string, UpdateTarget>();

        public StorageEntry(string name, string type, string path, Guid guid)
        {
            Guid = guid;
            Name = name;
            Type = type;
            Path = path;
        }
    }
}