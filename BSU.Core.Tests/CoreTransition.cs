using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BSU.Core.Model.Actions;
using BSU.Core.Services;
using Xunit;

namespace BSU.Core.Tests
{
    public class CoreTransition : IDisposable
    {
        private DirectoryInfo _tmpDir;

        public CoreTransition()
        {
            _tmpDir = new DirectoryInfo(Path.GetTempPath()).CreateSubdirectory(Guid.NewGuid().ToString());
        }

        public void Dispose()
        {
            _tmpDir.Delete(true);
        }


        private static void AddRepo(ISettings settings, string name)
        {
            settings.Repositories.Add(new RepoEntry
            {
                Name = name,
                Type = "MOCK",
                Url = "url/" + name
            });
        }

        private static void AddStorage(ISettings settings, string name)
        {
            settings.Storages.Add(new StorageEntry
            {
                Name = name,
                Path = "path/" + name,
                Type = "TMPBACKED",
                Updating = new Dictionary<string, UpdateTarget>()
            });
        }

        private string GetVersionHash(string version)
        {
            var mod = new MockRepositoryMod();
            mod.SetFile("Common2", "common2");
            mod.SetFile("Common1", "common1");
            mod.SetFile("Version", version);
            return new Hashes.VersionHash(mod).GetHashString();
        }

        private (ISettings, Model.Model) CommonSetup(Action<MockRepo>[] setupRepoFuncs = null, Action<TmpBackedStorage, ISettings>[] setupStorageFuncs = null)
        {
            var settings = new MockSettings();
            var types = new Types();
            
            types.AddRepoType("MOCK", url =>
            {
                var repo = new MockRepo(url);
                if (setupRepoFuncs == null) return repo;
                foreach (var setupRepoFunc in setupRepoFuncs)
                {
                    setupRepoFunc(repo);                    
                }
                return repo;
            });
            types.AddStorageType("TMPBACKED", path =>
            {
                var storage = new TmpBackedStorage(_tmpDir);
                if (setupStorageFuncs == null) return storage;
                foreach (var setupStorageFunc in setupStorageFuncs)
                {
                    setupStorageFunc(storage, settings);                    
                }
                return storage;
            });
            AddRepo(settings, "test_repo");
            AddStorage(settings, "test_storage");
            var state = new InternalState(settings, types);
            ServiceProvider.InternalState = state;
            var model = new Model.Model(state);
            model.Load();
            while (ServiceProvider.JobManager.GetActiveJobs().Any())
            {
                Thread.Sleep(500);
            }
            return (settings, model);
        }

        private Action<MockRepo> AddRepositoryMod(string version)
        {
            return repo =>
            {
                var repoMod = new MockRepositoryMod {Identifier = "repo_test"};
                repo.Mods.Add("repo_test", repoMod);
                repoMod.SetFile("Common1", "common1");
                repoMod.SetFile("Common2", "common2");
                repoMod.SetFile("Version", version);
            };
        }

        private Action<TmpBackedStorage, ISettings> AddStorageMod(string currentVersion, string newVersion = null)
        {
            return (storage, settings) =>
            {
                var storageMod = storage.CreateMod("storage_test") as TmpBackedStorageMod;
                storageMod.SetFile("Common1", "common1");
                storageMod.SetFile("Common2", "common2");
                storageMod.SetFile("Version", currentVersion);
                if (newVersion != null)
                    settings.Storages[0].Updating
                        .Add("storage_test", new UpdateTarget(GetVersionHash(newVersion), newVersion));
            };
        }


        [Fact]
        private void Download()
        {
            var (settings, model) = CommonSetup(new[] {AddRepositoryMod("my_version")});

            var selectedAction = model.Repositories.Single().Mods.Single().Actions.Values.OfType<DownloadAction>().Single();
            selectedAction.FolderName = "storage_mod";
            var update = model.PrepareUpdate(new List<ModAction> {selectedAction});
            model.DoUpdate(update);
            while (!update.IsDone())
            {
                // TODO: sleep here, check state during update?
                Thread.Sleep(10);
            }

            Assert.False(update.HasError());

            //Assert.Equal("storage_mod", settings.Storages.Single().Updating.Keys.Single());
            //Assert.Equal(GetVersionHash("my_version"), settings.Storages.Single().Updating["storage_mod"].Hash);
            var useAction = model.Repositories.Single().Mods.Single().Actions.Values.OfType<UseAction>().SingleOrDefault();
            Assert.NotNull(useAction);
            var awaitAction = model.Repositories.Single().Mods.Single().Actions.Values.OfType<AwaitUpdateAction>().SingleOrDefault();
            Assert.Null(awaitAction);
            var updateAction = model.Repositories.Single().Mods.Single().Actions.Values.OfType<UpdateAction>().SingleOrDefault();
            Assert.Null(updateAction);
        }
        
        [Fact]
        private void ContinueUpdate()
        {
            var (settings, model) = CommonSetup(new[] {AddRepositoryMod("my_version")},
                new[] {AddStorageMod("old_version", "my_version")});
            
            var mod = model.Repositories.Single().Mods.Single();
            var action = mod.Actions.Select(kv => kv.Value).OfType<UpdateAction>().Single();
            Assert.Equal(GetVersionHash("my_version"), action.UpdateTarget.Hash);

            var update = model.PrepareUpdate(new List<ModAction>{action});
            model.DoUpdate(update);
            while (!update.IsDone())
            {
                Thread.Sleep(10);
            }

            Assert.False(update.HasError());

            Assert.Null(settings.Storages.Single().Updating.Keys.SingleOrDefault());
            var useAction = model.Repositories.Single().Mods.Single().Actions.Values.OfType<UseAction>().SingleOrDefault();
            Assert.NotNull(useAction);
            var awaitAction = model.Repositories.Single().Mods.Single().Actions.Values.OfType<AwaitUpdateAction>().SingleOrDefault();
            Assert.Null(awaitAction);
            var updateAction = model.Repositories.Single().Mods.Single().Actions.Values.OfType<UpdateAction>().SingleOrDefault();
            Assert.Null(updateAction);
        }
        
        [Fact]
        private void Update()
        {
            var (settings, model) =
                CommonSetup(new[] {AddRepositoryMod("my_version")},
                    new[] {AddStorageMod("old_version")});


            var selectedAction = model.Repositories.Single().Mods.Single().Actions.Values.OfType<UpdateAction>().Single();

            var mod = model.Repositories.Single().Mods.Single();
            var action = mod.Actions.Values.OfType<UpdateAction>().Single();
            Assert.Equal(GetVersionHash("my_version"), action.UpdateTarget.Hash);

            var update = model.PrepareUpdate(new List<ModAction> {action});
            model.DoUpdate(update);
            while (!update.IsDone())
            {
                Thread.Sleep(10);
            }

            Assert.False(update.HasError());

            Assert.Equal("storage_test", settings.Storages.Single().Updating.Keys.Single());
            Assert.Equal(GetVersionHash("my_version"), settings.Storages.Single().Updating["storage_test"].Hash);
            var useAction = model.Repositories.Single().Mods.Single().Actions.Values.OfType<UseAction>().SingleOrDefault();
            Assert.NotNull(useAction);
            var awaitAction = model.Repositories.Single().Mods.Single().Actions.Values.OfType<AwaitUpdateAction>().SingleOrDefault();
            Assert.Null(awaitAction);
            var updateAction = model.Repositories.Single().Mods.Single().Actions.Values.OfType<UpdateAction>().SingleOrDefault();
            Assert.Null(updateAction);
        }

        [Fact]
        private void AwaitUpdate()
        {
            var (settings, model) = CommonSetup(new[]{AddRepositoryMod( "my_version")},
                new []{AddStorageMod("old_version")});
            var repoMod = model.Repositories.Single().Mods.Single().Implementation as MockRepositoryMod;
            repoMod.SleepMs = 10000;


            var selectedAction = model.Repositories.Single().Mods.Single().Actions.Values.OfType<UpdateAction>().Single();

            var update = model.PrepareUpdate(new List<ModAction> {selectedAction});
            model.DoUpdate(update);

            var mod = model.Repositories.Single().Mods.Single();
            var action = mod.Actions.Values.OfType<AwaitUpdateAction>().Single();
            Assert.Equal(GetVersionHash("my_version"), action.UpdateTarget.Hash);

            repoMod.SleepMs = 0;
            while (!update.IsDone())
            {
                Thread.Sleep(10);
            }

            Assert.False(update.HasError());

            Assert.Empty(settings.Storages.Single().Updating);
            var useAction = model.Repositories.Single().Mods.Single().Actions.Values.OfType<UseAction>().SingleOrDefault();
            Assert.NotNull(useAction);
            var storageMod = model.Storages.Single().Mods.Single().Implementation as TmpBackedStorageMod;
            Assert.Equal("my_version", storageMod.GetFileContent("Version"));
            Assert.NotNull(model.Storages.Single().Mods.SingleOrDefault());
            var awaitAction = model.Repositories.Single().Mods.Single().Actions.Values.OfType<AwaitUpdateAction>().SingleOrDefault();
            Assert.Null(awaitAction);
            var updateAction = model.Repositories.Single().Mods.Single().Actions.Values.OfType<UpdateAction>().SingleOrDefault();
            Assert.Null(updateAction);
        }

        [Fact]
        private void AwaitDownload()
        {
            var (settings, model) = CommonSetup(new []{AddRepositoryMod("my_version")});
            var repoMod = model.Repositories.Single().Mods.Single().Implementation as MockRepositoryMod;
            repoMod.SleepMs = 10000;


            var selectedAction = model.Repositories.Single().Mods.Single().Actions.Values.OfType<DownloadAction>().Single();
            selectedAction.FolderName = "storage_mod";

            var update = model.PrepareUpdate(new List<ModAction>{selectedAction});
            model.DoUpdate(update);

            var mod = model.Repositories.Single().Mods.Single();
            var action = mod.Actions.Values.OfType<AwaitUpdateAction>().Single();
            Assert.Equal(GetVersionHash("my_version"), action.UpdateTarget.Hash);

            repoMod.SleepMs = 0;
            while (!update.IsDone())
            {
                Thread.Sleep(10);
            }

            Assert.False(update.HasError());

            Assert.Empty(settings.Storages.Single().Updating);
            var useAction = model.Repositories.Single().Mods.Single().Actions.Values.OfType<UseAction>().SingleOrDefault();
            Assert.NotNull(useAction);
            var storageMod = model.Storages.Single().Mods.Single().Implementation as TmpBackedStorageMod;
            Assert.Equal("my_version", storageMod.GetFileContent("Version"));
            Assert.NotNull(model.Storages.Single().Mods.SingleOrDefault());
            var awaitAction = model.Repositories.Single().Mods.Single().Actions.Values.OfType<AwaitUpdateAction>().SingleOrDefault();
            Assert.Null(awaitAction);
            var updateAction = model.Repositories.Single().Mods.Single().Actions.Values.OfType<UpdateAction>().SingleOrDefault();
            Assert.Null(updateAction);
        }
    }
}