using System.IO;
using Xunit;

namespace BSU.Core.Tests
{
    public class CoreStories
    {
        private readonly Core _core;
        private readonly MockSettings _settings;
        public CoreStories()
        {
            _settings = new MockSettings();
            _core = new Core(_settings);
            _core.AddRepoType("MOCK", (name, url) => new MockRepo(name, url));
            _core.AddStorageType("MOCK", (name, path) => new MockStorage(name, path));
        }

        private MockRepo AddRepo(string name)
        {
            _core.AddRepo(name, "url/" + name, "MOCK");
            return MockRepo.Repos[name];
        }

        private MockStorage AddStorage(string name)
        {
            _core.AddStorage(name, new DirectoryInfo("path/" + name), "MOCK");
            return MockStorage.Storages[name];
        }

        [Fact]
        public void Setup()
        {
            var repo = AddRepo("test_repo");
            var remoteMod = new MockRemoteMod();
            repo.Mods.Add(remoteMod);
            var storage = AddStorage("test_storage");
            var localMod = new MockStorageMod
            {
                Identifier = "test_local_mod"
            };
            storage.Mods.Add(localMod);
            var state = _core.GetState();
            Assert.Single(state.Repos, r => r.Name == "test_repo");
            Assert.Single(state.Storages, s => s.Name == "test_storage");
        }
    }
}
