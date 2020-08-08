using System;
using System.Collections.Generic;
using System.Linq;
using BSU.CoreCommon;

namespace BSU.Core.Tests.Mocks
{
    internal class MockStorage : IStorage
    {
        private string path;

        public Dictionary<string, MockStorageMod> Mods = new Dictionary<string, MockStorageMod>();

        public MockStorage()
        {
        }

        public bool CanWrite() => true;

        public Dictionary<string, IStorageMod> GetMods() =>
            Mods.ToDictionary(kv => kv.Key, kv => (IStorageMod) kv.Value);

        public IStorageMod CreateMod(string identifier)
        {
            if (identifier == null) throw new ArgumentNullException();
            var newMod = new MockStorageMod {Identifier = identifier, Storage = this};
            Mods.Add(identifier, newMod);
            return newMod;
        }

        public void RemoveMod(string identifier)
        {
            Mods.Remove(identifier);
        }

        public Uid GetUid() => new Uid();

        public void Load()
        {
            
        }
    }
}
