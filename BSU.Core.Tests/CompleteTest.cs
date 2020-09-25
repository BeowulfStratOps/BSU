using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BSU.Core.Persistence;
using BSU.Core.Tests.Mocks;
using BSU.CoreCommon;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests
{
    public class CompleteTest : LoggedTest
    {
        public CompleteTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        private MockRepositoryMod CreateRepoMod(string match, string version)
        {
            return new MockRepositoryMod(mod =>
            {
                for (int i = 0; i < 3; i++)
                {
                    mod.SetFile($"/addons/file_{match}_{i}.pbo", $"data_{version}_{i}");
                }
            });
        }
        
        private MockStorageMod CreateStorageMod(string match, string version)
        {
            return new MockStorageMod(mod =>
            {
                for (int i = 0; i < 3; i++)
                {
                    mod.SetFile($"/addons/file_{match}_{i}.pbo", $"data_{version}_{i}");
                }
            });
        }

        private IRepository CreateRepo(string url)
        {
            return new MockRepository(repo =>
            {
                if (url == "r1")
                {
                    repo.Mods.Add("mod1", CreateRepoMod("r1", "1"));
                    //repo.Mods.Add("mod2", CreateRepoMod("r2", "1"));
                }
                /*else
                {
                    repo.Mods.Add("mod3", CreateRepoMod("r3", "1"));
                    repo.Mods.Add("mod4", CreateRepoMod("r4", "1"));
                }*/
            });
        }

        private IStorage CreateStorage(string path)
        {
            return new MockStorage(storage =>
            {
                if (path == "s1")
                {
                    storage.Mods.Add("mod5", CreateStorageMod("r1", "1"));
                    //storage.Mods.Add("mod6", CreateStorageMod("s2", "1")); // TODO: critical line!!
                }
                /*else
                {
                    storage.Mods.Add("mod7", CreateStorageMod("s3", "1"));
                    storage.Mods.Add("mod8", CreateStorageMod("s4", "1"));
                }*/
            });
        }

        [Fact]
        private void Load()
        {
            var worker = new MockWorker();
            var types = new Types();
            types.AddRepoType("mock", CreateRepo);
            types.AddStorageType("mock", CreateStorage);
            var settings = new MockSettings();
            settings.Repositories.Add(new RepositoryEntry("repo1", "mock", "r1"));
            //settings.Repositories.Add(new RepositoryEntry("repo2", "mock", "r2"));
            settings.Storages.Add(new StorageEntry("storage1", "mock", "s1"));
            //settings.Storages.Add(new StorageEntry("storage2", "mock", "s2"));
            var state = new InternalState(settings);
            var model = new Model.Model(state, worker, types, worker);
            model.Load();
            worker.DoWork();
            var storageMod1 = model.Storages.Single(s => s.Name == "storage1").Mods
                .Single(m => m.Identifier == "mod5");
            var repoMod1 = model.Repositories.Single(s => s.Name == "repo1").Mods
                .Single(m => m.Identifier == "mod1");
            Assert.Equal(storageMod1, repoMod1.Selection.StorageMod);
        }
    }

    internal class MockRepository : IRepository
    {
        public Dictionary<string, IRepositoryMod> Mods { get; set; } = new Dictionary<string, IRepositoryMod>();
        private readonly Action<MockRepository> _load;

        public MockRepository(Action<MockRepository> load)
        {
            _load = load;
        }

        public void Load() => _load(this);

        public Dictionary<string, IRepositoryMod> GetMods() => Mods;
    }
}