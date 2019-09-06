using System;
using System.Collections.Generic;
using System.IO;
using BSU.CoreInterface;

namespace BSU.Core
{
    public class DirectoryStorage : IStorage
    {
        private string _path, _name;

        public DirectoryStorage(string path, string name)
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
