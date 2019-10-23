using System.Collections.Generic;
using System.Linq;
using BSU.CoreInterface;

namespace BSU.Core.Tests
{
    internal class MockRepo : IRepository
    {
        private string name;
        private string url;

        public List<MockRemoteMod> Mods = new List<MockRemoteMod>();

        public static Dictionary<string, MockRepo> Repos = new Dictionary<string, MockRepo>();

        public MockRepo(string name, string url)
        {
            this.name = name;
            this.url = url;
            Repos[name] = this;
        }

        public string GetLocation() => url;

        public List<IRemoteMod> GetMods() => Mods.OfType<IRemoteMod>().ToList();

        public string GetName() => name;
    }
}