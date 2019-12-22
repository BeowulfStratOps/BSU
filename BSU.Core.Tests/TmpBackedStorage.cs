using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.CoreCommon;

namespace BSU.Core.Tests
{
    internal class TmpBackedStorage : IStorage
    {
        private string name;
        private DirectoryInfo _baseTmp;

        public List<TmpBackedStorageMod> Mods = new List<TmpBackedStorageMod>();

        public TmpBackedStorage(string name, DirectoryInfo baseTmp)
        {
            this.name = name;
            _baseTmp = baseTmp.CreateSubdirectory(name + Guid.NewGuid());
        }

        public bool CanWrite() => true;

        public string GetLocation() => _baseTmp.FullName;

        public List<IStorageMod> GetMods() => Mods.OfType<IStorageMod>().ToList();

        public string GetIdentifier() => name;
        public IStorageMod CreateMod(string identifier)
        {
            if (identifier == null) throw new ArgumentNullException();
            var newMod = new TmpBackedStorageMod(_baseTmp, identifier) { Storage = this };
            Mods.Add(newMod);
            return newMod;
        }

        public Uid GetUid() => new Uid();
    }
}
