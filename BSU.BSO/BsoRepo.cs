using System;
using System.Collections.Generic;
using BSU.CoreInterface;

namespace BSU.BSO
{
    public class BsoRepo : IRepository
    {
        private string _url, _name;

        public BsoRepo(string url, string name)
        {
            _url = url;
            _name = name;
        }

        public List<IRemoteMod> GetMods()
        {
            throw new NotImplementedException();
        }

        public string GetName() => _name;

        public string GetLocation() => _url;
    }
}
