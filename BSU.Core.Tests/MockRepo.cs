using System.Collections.Generic;
using System.Linq;
using BSU.CoreCommon;

namespace BSU.Core.Tests
{
    internal class MockRepo : IRepository
    {
        private string name;
        private string url;

        public List<MockRepositoryMod> Mods = new List<MockRepositoryMod>();

        public MockRepo(string name, string url)
        {
            this.name = name;
            this.url = url;
        }

        public string GetLocation() => url;

        public List<IRepositoryMod> GetMods() => Mods.OfType<IRepositoryMod>().ToList();

        public string GetName() => name;
    }
}
