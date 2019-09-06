using System;
using System.Collections.Generic;
using System.IO;
using BSU.CoreInterface;

namespace BSU.Core
{
    public class DirectoryStorage : IStorage
    {
        public DirectoryStorage(string path, string name)
        {

        }

        public List<ILocalMod> GetMods()
        {
            throw new NotImplementedException();
        }

        public string GetLocation()
        {
            throw new NotImplementedException();
        }

        public string GetName()
        {
            throw new NotImplementedException();
        }
    }
}
