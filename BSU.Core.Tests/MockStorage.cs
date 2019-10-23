using System.Collections.Generic;
using System.Linq;
using BSU.CoreInterface;

namespace BSU.Core.Tests
{
    internal class MockStorage : IStorage
    {
        private string name;
        private string path;

        public List<MockStorageMod> Mods = new List<MockStorageMod>();

        public static Dictionary<string, MockStorage> Storages = new Dictionary<string, MockStorage>();

        public MockStorage(string name, string path)
        {
            this.name = name;
            this.path = path;
            Storages[name] = this;
        }

        public bool CanWrite() => true;

        public string GetLocation() => path;

        public List<ILocalMod> GetMods() => Mods.OfType<ILocalMod>().ToList();

        public string GetName() => name;
    }
}