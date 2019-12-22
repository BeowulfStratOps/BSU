using System.Collections.Generic;
using BSU.CoreCommon;

namespace BSU.Core
{
    internal class ErrorRepo : IRepository
    {
        private readonly string _name;
        private readonly string _url;
        private readonly string _errorMessage;

        public ErrorRepo(string name, string url, string errorMessage)
        {
            _name = name;
            _url = url;
            _errorMessage = errorMessage;
        }

        public List<IRepositoryMod> GetMods() => new List<IRepositoryMod>();

        public string GetName() => _name;

        public string GetLocation() => _url;
        public Uid GetUid()
        {
            throw new System.NotImplementedException();
        }
    }
}
