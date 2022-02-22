using System;
using System.Collections.Generic;
using BSU.Core.Launch;

namespace BSU.Core.Persistence
{
    internal interface IRepositoryEntry
    {
        Guid Guid { get; }
        string Name { get; }
        string Type { get; }
        string Url { get; }
    }

    internal class RepositoryEntry : IRepositoryEntry
    {
        public Guid Guid { get; }
        public string Name { get; }
        public string Type { get; }
        public string Url { get; }


        public Dictionary<string, PersistedSelection> UsedMods = new();

        public RepositoryEntry(string name, string type, string url, Guid guid)
        {
            Guid = guid;
            Name = name;
            Type = type;
            Url = url;
        }
    }
}
