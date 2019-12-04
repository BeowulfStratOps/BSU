using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.Core.State;
using Xunit;

namespace BSU.Core.Tests
{
    public class CoreTransition
    {
        private MockRepo AddRepo(Core core, string name)
        {
            core.AddRepo(name, "url/" + name, "MOCK");
            return MockRepo.Repos[name];
        }

        private MockStorage AddStorage(Core core, string name)
        {
            core.AddStorage(name, new DirectoryInfo("path/" + name), "MOCK");
            return MockStorage.Storages[name];
        }

        private string GetVersionHash(string version)
        {
            var mod = new MockRemoteMod();
            mod.Files.Add("Common2", "common2");
            mod.Files.Add("Common1", "common1");
            mod.Files.Add("Version", version);
            return new Hashes.VersionHash(mod).GetHashString();
        }

        private (MockSettings, Core, MockRepo, MockStorage) CommonSetup()
        {
            var settings = new MockSettings();
            var core = new Core(settings);
            core.AddRepoType("MOCK", (name, url) => new MockRepo(name, url));
            core.AddStorageType("MOCK", (name, path) => new MockStorage(name, path));
            var repo = AddRepo(core, "test_repo");
            var storage = AddStorage(core, "test_storage");
            return (settings, core, repo, storage);
        }

        private MockRemoteMod AddRemoteMod(MockRepo repo, string version)
        {
            var remoteMod = new MockRemoteMod {Identifier = "remote_test"};
            repo.Mods.Add(remoteMod);
            remoteMod.Files.Add("Common1", "common1");
            remoteMod.Files.Add("Common2", "common2");
            remoteMod.Files.Add("Version", version);
            return remoteMod;
        }

        private MockStorageMod AddLocalMod(MockStorage storage, string version)
        {
            var localMod = new MockStorageMod {Identifier = "local_test"};
            localMod.Files.Add("Common1", "common1");
            localMod.Files.Add("Common2", "common2");
            localMod.Files.Add("Version", version);
            storage.Mods.Add(localMod);
            return localMod;
        }


        [Fact]
        private void Download()
        {
            var (settings, core, repo, storage) = CommonSetup();
            AddRemoteMod(repo, "my_version");

            var state = core.GetState();

            var mod = state.Repos.Single().Mods.Single();
            var action = mod.Actions.OfType<DownloadAction>().Single();
            action.FolderName = "test_folder";
            mod.Selected = action;

            var update = state.Repos.Single().PrepareUpdate();
            update.DoUpdate();

            Assert.Equal("test_folder", storage.Mods.Single().Identifier);
            Assert.Equal("test_folder", settings.Storages.Single().Updating.Keys.Single());
            Assert.Equal(GetVersionHash("my_version"), settings.Storages.Single().Updating["test_folder"].Hash);
            state = core.GetState();
            var awaitAction = state.Repos.Single().Mods.Single().Actions.OfType<AwaitUpdateAction>().SingleOrDefault();
            Assert.NotNull(awaitAction);
            Assert.Equal(awaitAction.VersionHash, GetVersionHash("my_version"));
            Assert.NotNull(awaitAction.LocalMod);
            Assert.NotNull(storage.Mods.SingleOrDefault());
        }


        [Fact]
        private void Update()
        {
            var (settings, core, repo, storage) = CommonSetup();
            AddRemoteMod(repo, "my_version");
            AddLocalMod(storage, "old_version");

            var state = core.GetState();

            var mod = state.Repos.Single().Mods.Single();
            var action = mod.Actions.OfType<UpdateAction>().Single();
            Assert.Equal(GetVersionHash("my_version"), action.Target.Hash);
            mod.Selected = action;

            var update = state.Repos.Single().PrepareUpdate();
            update.DoUpdate();

            Assert.Equal("local_test", settings.Storages.Single().Updating.Keys.Single());
            Assert.Equal(GetVersionHash("my_version"), settings.Storages.Single().Updating["local_test"].Hash);
            state = core.GetState();
            var awaitAction = state.Repos.Single().Mods.Single().Actions.OfType<AwaitUpdateAction>().SingleOrDefault();
            Assert.NotNull(awaitAction);
            Assert.Equal(GetVersionHash("my_version"), awaitAction.VersionHash);
            Assert.NotNull(awaitAction.LocalMod);
            Assert.NotNull(storage.Mods.SingleOrDefault());
        }

        [Fact]
        private void AwaitUpdate()
        {
            var (settings, core, repo, storage) = CommonSetup();
            AddRemoteMod(repo, "my_version");
            var localMod = AddLocalMod(storage, "old_version");
            settings.Storages.Single().Updating["local_test"] = new UpdateTarget(GetVersionHash("my_version"), "my_version");

            var state = core.GetState();

            var mod = state.Repos.Single().Mods.Single();
            var action = mod.Actions.OfType<AwaitUpdateAction>().Single();
            Assert.Equal(GetVersionHash("my_version"), action.VersionHash);
            mod.Selected = action;

            var update = state.Repos.Single().PrepareUpdate();
            update.DoUpdate();

            Assert.Empty(settings.Storages.Single().Updating);
            state = core.GetState();
            var useAction = state.Repos.Single().Mods.Single().Actions.OfType<UseAction>().SingleOrDefault();
            Assert.NotNull(useAction);
            Assert.Equal("my_version", localMod.Files["Version"]);
            Assert.NotNull(storage.Mods.SingleOrDefault());
        }
    }
}
