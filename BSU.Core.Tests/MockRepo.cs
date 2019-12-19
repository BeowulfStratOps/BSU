using System.Collections.Generic;
using System.Linq;
using BSU.CoreCommon;

namespace BSU.Core.Tests
{
    internal class MockRepo : IRepository
    {
        private string name;
        private string url;

        public List<MockRemoteMod> Mods = new List<MockRemoteMod>();

        public MockRepo(string name, string url)
        {
            this.name = name;
            this.url = url;
        }

        public string GetLocation() => url;

        public List<IRemoteMod> GetMods() => Mods.OfType<IRemoteMod>().ToList();

        public string GetName() => name;
    }
}
