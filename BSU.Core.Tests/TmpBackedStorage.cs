using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.CoreCommon;

namespace BSU.Core.Tests
{
    internal class TmpBackedStorage : IStorage
    {
        private DirectoryInfo _baseTmp;

        public Dictionary<string, TmpBackedStorageMod> Mods = new Dictionary<string, TmpBackedStorageMod>();

        public TmpBackedStorage(DirectoryInfo baseTmp)
        {
            _baseTmp = baseTmp.CreateSubdirectory(Guid.NewGuid().ToString());
        }

        public bool CanWrite() => true;

        public string GetLocation() => _baseTmp.FullName;

        public Dictionary<string, IStorageMod> GetMods() =>
            Mods.ToDictionary(kv => kv.Key, kv => (IStorageMod) kv.Value);

        public IStorageMod CreateMod(string identifier)
        {
            if (identifier == null) throw new ArgumentNullException();
            var newMod = new TmpBackedStorageMod(_baseTmp, identifier) {Storage = this};
            Mods.Add(identifier, newMod);
            return newMod;
        }

        public void RemoveMod(string identifier)
        {
            Mods.Remove(identifier);
            // TODO: remove folder?
        }

        public Uid GetUid() => new Uid();

        public void Load()
        {
            
        }
    }
}
