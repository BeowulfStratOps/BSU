using System;
using System.Collections.Generic;
using System.IO;
using BSU.CoreInterface;

namespace BSU.Core
{
    public class SteamStorage : IStorage
    {
        private string _path, _name;
        public SteamStorage(string path, string name)
        {
            _path = path;
            _name = name;
        }

        public List<ILocalMod> GetMods()
        {
            throw new NotImplementedException();
        }

        public string GetLocation() => _path;

        public string GetName() => _name;
    }
}