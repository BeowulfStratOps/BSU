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
    public class ComplexLoadingTest : LoggedTest
    {
        public ComplexLoadingTest(ITestOutputHelper outputHelper) : base(outputHelper)
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
        
        private MockStorageMod CreateStorageMod(string match, string version, int delayMs = 0)
        {
            return new MockStorageMod(mod =>
            {
                Thread.Sleep(delayMs);
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
                }
                else
                {
                    repo.Mods.Add("mod2", CreateRepoMod("r1", "2"));
                }
            });
        }

        private IStorage CreateStorage(string path)
        {
            return new MockStorage(storage =>
            {
                storage.Mods.Add("mod3", CreateStorageMod("r1", "0"));
            });
        }

        [Fact]
        private void LoadUsed1()
        {
            var worker = new MockWorker();
            var types = new Types();
            types.AddRepoType("mock", CreateRepo);
            types.AddStorageType("mock", CreateStorage);
            var settings = new MockSettings();
            var storageEntry = new StorageEntry("storage1", "mock", "s1", Guid.NewGuid());
            settings.Storages.Add(storageEntry);
            var repoEntry1 = new RepositoryEntry("repo1", "mock", "r1", Guid.NewGuid());
            repoEntry1.UsedMods.Add("mod1", new PersistedSelection(storageEntry.Guid, "mod3"));
            settings.Repositories.Add(repoEntry1);
            settings.Repositories.Add(new RepositoryEntry("repo2", "mock", "r2", Guid.NewGuid()));
            var state = new InternalState(settings);
            var model = new Model.Model(state, worker, types, worker);
            model.Load();
            worker.DoWork();
            var repoMod1 = model.Repositories.Single(s => s.Name == "repo1").Mods
                .Single(m => m.Identifier == "mod1");
            var repoMod2 = model.Repositories.Single(s => s.Name == "repo2").Mods
                .Single(m => m.Identifier == "mod2");
            Assert.NotNull(repoMod1.Selection.StorageMod);
            Assert.Null(repoMod2.Selection); // could update but has conflicts. could also download -> not sure what to do.
        }
        
        /// <summary>
        /// Used repos should be selected as soon as they are loaded, regardless of other mods
        /// </summary>
        [Fact]
        private void LoadUsed1_Shortcircuit()
        {
            var backgroundWorker = new JobManager.JobManager();
            var worker = new MockWorker();
            var types = new Types();
            types.AddRepoType("mock", url => new MockRepository(repo =>
            {
                repo.Mods.Add("mod1", CreateRepoMod("r1", "1"));
                repo.Mods.Add("mod2", CreateRepoMod("r2", "1"));
            }));
            types.AddStorageType("mock", path =>
                new MockStorage(storage =>
                    {
                        storage.Mods.Add("mod3", CreateStorageMod("r1", "0"));
                        storage.Mods.Add("mod4", CreateStorageMod("r2", "0", 5000));
                    }
                ));
            var settings = new MockSettings();
            var storageEntry = new StorageEntry("storage1", "mock", "s1", Guid.NewGuid());
            settings.Storages.Add(storageEntry);
            var repoEntry1 = new RepositoryEntry("repo1", "mock", "r1", Guid.NewGuid());
            repoEntry1.UsedMods.Add("mod1", new PersistedSelection(storageEntry.Guid, "mod3"));
            settings.Repositories.Add(repoEntry1);
            settings.Repositories.Add(new RepositoryEntry("repo2", "mock", "r2", Guid.NewGuid()));
            var state = new InternalState(settings);
            var model = new Model.Model(state, backgroundWorker, types, worker);
            model.Load();

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 2)
            {                
                worker.DoWork();                
            }
            
            var repoMod1 = model.Repositories.Single(s => s.Name == "repo1").Mods
                .Single(m => m.Identifier == "mod1");
            var repoMod2 = model.Repositories.Single(s => s.Name == "repo2").Mods
                .Single(m => m.Identifier == "mod2");
            Assert.NotNull(repoMod1.Selection?.StorageMod);
            Assert.Null(repoMod2.Selection);
            
            backgroundWorker.Shutdown(true);
        }
        
        [Fact]
        private void LoadUsed2()
        {
            var worker = new MockWorker();
            var types = new Types();
            types.AddRepoType("mock", CreateRepo);
            types.AddStorageType("mock", CreateStorage);
            var settings = new MockSettings();
            var storageEntry = new StorageEntry("storage1", "mock", "s1", Guid.NewGuid());
            settings.Storages.Add(storageEntry);
            var repoEntry1 = new RepositoryEntry("repo1", "mock", "r1", Guid.NewGuid());
            repoEntry1.UsedMods.Add("mod1", new PersistedSelection(storageEntry.Guid, "mod3"));
            settings.Repositories.Add(repoEntry1);
            var repoEntry2 = new RepositoryEntry("repo2", "mock", "r2", Guid.NewGuid());
            repoEntry2.UsedMods.Add("mod2", new PersistedSelection(storageEntry.Guid, "mod3"));
            settings.Repositories.Add(repoEntry2);
            var state = new InternalState(settings);
            var model = new Model.Model(state, worker, types, worker);
            model.Load();
            worker.DoWork();
            var repoMod1 = model.Repositories.Single(s => s.Name == "repo1").Mods
                .Single(m => m.Identifier == "mod1");
            var repoMod2 = model.Repositories.Single(s => s.Name == "repo2").Mods
                .Single(m => m.Identifier == "mod2");
            Assert.NotNull(repoMod1.Selection.StorageMod);
            Assert.NotNull(repoMod2.Selection.StorageMod);
        }
        
        [Fact]
        private void Load()
        {
            var worker = new MockWorker();
            var types = new Types();
            types.AddRepoType("mock", CreateRepo);
            types.AddStorageType("mock", CreateStorage);
            var settings = new MockSettings();
            settings.Repositories.Add(new RepositoryEntry("repo1", "mock", "r1", Guid.NewGuid()));
            settings.Repositories.Add(new RepositoryEntry("repo2", "mock", "r2", Guid.NewGuid()));
            settings.Storages.Add(new StorageEntry("storage1", "mock", "s1", Guid.NewGuid()));
            var state = new InternalState(settings);
            var model = new Model.Model(state, worker, types, worker);
            model.Load();
            worker.DoWork();
            var repoMod1 = model.Repositories.Single(s => s.Name == "repo1").Mods
                .Single(m => m.Identifier == "mod1");
            var repoMod2 = model.Repositories.Single(s => s.Name == "repo2").Mods
                .Single(m => m.Identifier == "mod2");
            Assert.Null(repoMod1.Selection);
            Assert.Null(repoMod2.Selection);
        }
    }
}