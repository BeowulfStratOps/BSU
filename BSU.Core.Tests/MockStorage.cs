using System;
using System.Collections.Generic;
using System.Linq;
using BSU.CoreCommon;

namespace BSU.Core.Tests
{
    internal class MockStorage : IStorage
    {
        private string name;
        private string path;

        public List<MockStorageMod> Mods = new List<MockStorageMod>();

        public MockStorage(string name, string path)
        {
            this.name = name;
            this.path = path;
        }

        public bool CanWrite() => true;

        public string GetLocation() => path;

        public List<IStorageMod> GetMods() => Mods.OfType<IStorageMod>().ToList();

        public string GetIdentifier() => name;

        public IStorageMod CreateMod(string identifier)
        {
            if (identifier == null) throw new ArgumentNullException();
            var newMod = new MockStorageMod {Identifier = identifier, Storage = this};
            Mods.Add(newMod);
            return newMod;
        }

        public void RemoveMod(string identifier)
        {
            Mods.RemoveAll(m => m.Identifier == identifier);
        }

        public Uid GetUid() => new Uid();
    }
}