using System.Collections.Generic;
using System.Linq;
using BSU.CoreCommon;

namespace BSU.Core.Tests
{
    internal class MockRepo : IRepository
    {
        public Dictionary<string, MockRepositoryMod> Mods = new Dictionary<string, MockRepositoryMod>();

        public MockRepo()
        {
        }
        public Uid GetUid() => new Uid();

        public void Load()
        {
            
        }

        public Dictionary<string, IRepositoryMod> GetMods() =>
            Mods.ToDictionary(kv => kv.Key, kv => (IRepositoryMod) kv.Value);
    }
}