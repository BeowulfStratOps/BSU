using System;
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

        public MockStorage(string name, string path)
        {
            this.name = name;
            this.path = path;
        }

        public bool CanWrite() => true;

        public string GetLocation() => path;

        public List<ILocalMod> GetMods() => Mods.OfType<ILocalMod>().ToList();

        public string GetIdentifier() => name;
        public ILocalMod CreateMod(string identifier)
        {
            if (identifier == null) throw new ArgumentNullException();
            var newMod = new MockStorageMod { Identifier = identifier, Storage = this };
            Mods.Add(newMod);
            return newMod;
        }
    }
}