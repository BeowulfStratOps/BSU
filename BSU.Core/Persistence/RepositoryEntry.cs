using System.Collections.Generic;

namespace BSU.Core.Persistence
{
    internal interface IRepositoryEntry
    {
        string Name { get; }
        string Type { get; }
        string Url { get; }
    }

    internal class RepositoryEntry : IRepositoryEntry
    {
        public string Name { get; }
        public string Type { get; }
        public string Url { get; }
        
        
        
        public Dictionary<string, PersistedSelection> UsedMods = new Dictionary<string, PersistedSelection>();

        public RepositoryEntry(string name, string type, string url)
        {
            Name = name;
            Type = type;
            Url = url;
        }
    }
}