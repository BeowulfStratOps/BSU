using System.Collections.Generic;

namespace BSU.Core.Persistence
{
    internal interface IStorageEntry
    {
        string Name { get; }
        string Type { get; }
        string Path { get; }
    }

    internal class StorageEntry : IStorageEntry
    {
        public string Name { get; }
        public string Type { get; }
        public string Path { get; }
        public Dictionary<string, UpdateTarget> Updating = new Dictionary<string, UpdateTarget>();

        public StorageEntry(string name, string type, string path)
        {
            Name = name;
            Type = type;
            Path = path;
        }
    }
}